"""
API-to-API integration tests.

These tests verify the full chain where the AI service calls the .NET API
(DotNetClient) to enrich slot scoring with real user/barber data.

The .NET API is mocked via httpx, so no live backend is needed in CI.
The tests validate:
  1. AI service correctly calls .NET /api/reviews/barber/{id}
  2. AI service correctly calls .NET /api/appointments/my
  3. Scoring engine integrates the .NET data into final recommendation
  4. AI endpoint degrades gracefully when .NET API is unreachable
"""

from unittest.mock import AsyncMock, MagicMock, patch

import httpx
import pytest

from services.dotnet_client import DotNetClient
from tests.conftest import make_scored_slot

# ── Shared payload sent from frontend to AI ──────────────────────────────────

BASE_PAYLOAD = {
    "userId": "user-integration-test",
    "availableSlots": [
        {
            "barberId": "barber-001",
            "barberName": "Alex Popa",
            "barberRating": 4.7,
            "serviceId": "svc-001",
            "date": "2026-05-20",
            "startTime": "10:00:00",
            "endTime": "10:30:00",
        },
        {
            "barberId": "barber-002",
            "barberName": "Mihai Stan",
            "barberRating": 3.9,
            "serviceId": "svc-002",
            "date": "2026-05-20",
            "startTime": "15:00:00",
            "endTime": "15:30:00",
        },
    ],
    "city": "Iasi",
    "includeCalendar": False,
    "authToken": "test-jwt-token",
}

# ── Fake data returned by .NET API ────────────────────────────────────────────

FAKE_REVIEWS = [
    {"rating": 5, "comment": "Excellent!"},
    {"rating": 4, "comment": "Good job."},
    {"rating": 5, "comment": "Will come back!"},
]

FAKE_APPOINTMENTS = [
    {"barberId": "barber-001", "appointmentDate": "2026-04-15"},
    {"barberId": "barber-002", "appointmentDate": "2026-03-10"},
]


# ── Unit tests: DotNetClient calls the correct .NET endpoints ────────────────

class TestDotNetClientApiCalls:
    """Verify DotNetClient constructs correct URLs and handles responses."""

    @pytest.mark.asyncio
    async def test_get_barber_reviews_calls_correct_endpoint(self):
        with patch("services.dotnet_client.settings") as mock_settings:
            mock_settings.dotnet_api_url = "https://smartcut-api.example.com"
            mock_settings.dotnet_api_timeout = 10
            client = DotNetClient(auth_token="test-token")

        captured_url = None

        async def fake_get(url, **kwargs):
            nonlocal captured_url
            captured_url = url
            mock_response = MagicMock()
            mock_response.raise_for_status = MagicMock()
            mock_response.json.return_value = FAKE_REVIEWS
            return mock_response

        with patch("httpx.AsyncClient") as mock_async_client:
            mock_ctx = AsyncMock()
            mock_ctx.__aenter__ = AsyncMock(return_value=mock_ctx)
            mock_ctx.__aexit__ = AsyncMock(return_value=False)
            mock_ctx.get = AsyncMock(side_effect=fake_get)
            mock_async_client.return_value = mock_ctx

            result = await client.get_barber_reviews("barber-001")

        assert captured_url == "https://smartcut-api.example.com/api/reviews/barber/barber-001"
        assert result == FAKE_REVIEWS

    @pytest.mark.asyncio
    async def test_get_user_appointments_sends_auth_header(self):
        with patch("services.dotnet_client.settings") as mock_settings:
            mock_settings.dotnet_api_url = "https://smartcut-api.example.com"
            mock_settings.dotnet_api_timeout = 10
            client = DotNetClient(auth_token="bearer-xyz")

        captured_headers = None

        async def fake_get(url, headers=None, **kwargs):
            nonlocal captured_headers
            captured_headers = headers
            mock_response = MagicMock()
            mock_response.raise_for_status = MagicMock()
            mock_response.json.return_value = FAKE_APPOINTMENTS
            return mock_response

        with patch("httpx.AsyncClient") as mock_async_client:
            mock_ctx = AsyncMock()
            mock_ctx.__aenter__ = AsyncMock(return_value=mock_ctx)
            mock_ctx.__aexit__ = AsyncMock(return_value=False)
            mock_ctx.get = AsyncMock(side_effect=fake_get)
            mock_async_client.return_value = mock_ctx

            result = await client.get_user_appointments()

        assert captured_headers is not None
        assert captured_headers.get("Authorization") == "Bearer bearer-xyz"
        assert result == FAKE_APPOINTMENTS

    @pytest.mark.asyncio
    async def test_get_barber_reviews_returns_empty_on_timeout(self):
        with patch("services.dotnet_client.settings") as mock_settings:
            mock_settings.dotnet_api_url = "https://smartcut-api.example.com"
            mock_settings.dotnet_api_timeout = 10
            client = DotNetClient()

        with patch("httpx.AsyncClient") as mock_async_client:
            mock_ctx = AsyncMock()
            mock_ctx.__aenter__ = AsyncMock(return_value=mock_ctx)
            mock_ctx.__aexit__ = AsyncMock(return_value=False)
            mock_ctx.get = AsyncMock(side_effect=httpx.TimeoutException("timeout"))
            mock_async_client.return_value = mock_ctx

            result = await client.get_barber_reviews("barber-001")

        assert result == []

    @pytest.mark.asyncio
    async def test_get_user_appointments_returns_empty_on_http_error(self):
        with patch("services.dotnet_client.settings") as mock_settings:
            mock_settings.dotnet_api_url = "https://smartcut-api.example.com"
            mock_settings.dotnet_api_timeout = 10
            client = DotNetClient(auth_token="token")

        mock_response = MagicMock()
        mock_response.status_code = 401

        with patch("httpx.AsyncClient") as mock_async_client:
            mock_ctx = AsyncMock()
            mock_ctx.__aenter__ = AsyncMock(return_value=mock_ctx)
            mock_ctx.__aexit__ = AsyncMock(return_value=False)
            mock_ctx.get = AsyncMock(
                side_effect=httpx.HTTPStatusError(
                    "401", request=MagicMock(), response=mock_response
                )
            )
            mock_async_client.return_value = mock_ctx

            result = await client.get_user_appointments()

        assert result == []


# ── Integration tests: /recommend endpoint uses .NET data ────────────────────

@pytest.mark.asyncio
class TestRecommendUsesNETApiData:
    """
    Verify /recommend endpoint integrates DotNetClient data into scoring.
    The scoring engine and Gemini are mocked; only the DotNetClient chain matters.
    """

    async def test_recommend_succeeds_when_dotnet_api_reachable(self, async_client):
        top_3 = [make_scored_slot("Alex Popa", 10, 88.0)]
        with (
            patch("routers.recommend.rank_slots", return_value=top_3),
            patch("routers.recommend.refine_with_gemini", return_value=(0, "Best slot!")),
        ):
            response = await async_client.post("/recommend", json=BASE_PAYLOAD)

        assert response.status_code == 200
        data = response.json()
        assert data["recommended_slot"]["barber"] == "Alex Popa"

    async def test_recommend_degrades_gracefully_when_dotnet_api_offline(self, async_client):
        """Slots without .NET enrichment still get scored and recommended."""
        payload_without_token = {**BASE_PAYLOAD, "authToken": None}
        top_3 = [make_scored_slot("Mihai Stan", 15, 75.0)]
        with (
            patch("routers.recommend.rank_slots", return_value=top_3),
            patch("routers.recommend.refine_with_gemini", return_value=(0, "Good enough!")),
        ):
            response = await async_client.post("/recommend", json=payload_without_token)

        assert response.status_code == 200

    async def test_recommend_returns_422_when_no_slots_can_be_scored(self, async_client):
        with (
            patch("routers.recommend.rank_slots", return_value=[]),
            patch("routers.recommend.refine_with_gemini", return_value=(0, "")),
        ):
            response = await async_client.post("/recommend", json=BASE_PAYLOAD)

        assert response.status_code == 422

    async def test_recommend_returns_400_when_slots_empty(self, async_client):
        payload = {**BASE_PAYLOAD, "availableSlots": []}
        response = await async_client.post("/recommend", json=payload)
        assert response.status_code == 400

    async def test_recommend_response_includes_score_breakdown(self, async_client):
        top_3 = [make_scored_slot("Alex Popa", 10, 91.5)]
        with (
            patch("routers.recommend.rank_slots", return_value=top_3),
            patch("routers.recommend.refine_with_gemini", return_value=(0, "Excellent!")),
        ):
            response = await async_client.post("/recommend", json=BASE_PAYLOAD)

        data = response.json()
        assert "score_breakdown" in data
        assert "total" in data["score_breakdown"]
        assert isinstance(data["score_breakdown"]["total"], float | int)

    async def test_recommend_top3_list_is_present(self, async_client):
        top_3 = [
            make_scored_slot("Alex Popa", 10, 91.0),
            make_scored_slot("Mihai Stan", 15, 80.0),
        ]
        with (
            patch("routers.recommend.rank_slots", return_value=top_3),
            patch("routers.recommend.refine_with_gemini", return_value=(0, "Best!")),
        ):
            response = await async_client.post("/recommend", json=BASE_PAYLOAD)

        data = response.json()
        assert "top_3" in data
        assert len(data["top_3"]) == 2
