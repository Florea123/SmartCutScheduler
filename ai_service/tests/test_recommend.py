"""
Integration tests for the /recommend and /health endpoints.
External services (scoring, Gemini) are mocked.
"""

from unittest.mock import AsyncMock, patch

import pytest

from tests.conftest import make_scored_slot

BASE_PAYLOAD = {
    "userId": "user-abc",
    "availableSlots": [
        {
            "barberId": "b1",
            "barberName": "Ion Popescu",
            "barberRating": 4.5,
            "serviceId": "s1",
            "date": "2026-04-10",
            "startTime": "09:00:00",
            "endTime": "09:30:00",
        },
        {
            "barberId": "b2",
            "barberName": "Andrei Ionescu",
            "barberRating": 3.8,
            "serviceId": "s2",
            "date": "2026-04-10",
            "startTime": "14:00:00",
            "endTime": "14:30:00",
        },
    ],
    "city": "Bucharest",
    "includeCalendar": False,
}


@pytest.mark.asyncio
class TestHealthEndpoint:
    async def test_health_returns_200(self, async_client):
        response = await async_client.get("/health")
        assert response.status_code == 200

    async def test_health_body(self, async_client):
        response = await async_client.get("/health")
        data = response.json()
        assert data["status"] == "ok"
        assert "service" in data


@pytest.mark.asyncio
class TestRecommendEndpoint:
    async def test_returns_200(self, async_client):
        top_3 = [make_scored_slot("Ion", 9, 85.0), make_scored_slot("Andrei", 14, 72.0)]
        with (
            patch("routers.recommend.rank_slots", return_value=top_3),
            patch("routers.recommend.refine_with_gemini", return_value=(0, "Great slot!")),
        ):
            response = await async_client.post("/recommend", json=BASE_PAYLOAD)
        assert response.status_code == 200

    async def test_response_has_required_fields(self, async_client):
        top_3 = [make_scored_slot("Ion", 9, 85.0)]
        with (
            patch("routers.recommend.rank_slots", return_value=top_3),
            patch("routers.recommend.refine_with_gemini", return_value=(0, "Best slot!")),
        ):
            response = await async_client.post("/recommend", json=BASE_PAYLOAD)
        data = response.json()
        assert "recommended_slot" in data
        assert "reason" in data
        assert "score_breakdown" in data
        assert "top_3" in data

    async def test_recommended_slot_fields(self, async_client):
        top_3 = [make_scored_slot("Ion", 9, 85.0)]
        with (
            patch("routers.recommend.rank_slots", return_value=top_3),
            patch("routers.recommend.refine_with_gemini", return_value=(0, "Best!")),
        ):
            response = await async_client.post("/recommend", json=BASE_PAYLOAD)
        slot = response.json()["recommended_slot"]
        assert "date" in slot
        assert "time" in slot
        assert "barber" in slot
        assert "barberId" in slot
        assert "serviceId" in slot

    async def test_empty_slots_returns_422(self, async_client):
        payload = {**BASE_PAYLOAD, "availableSlots": []}
        response = await async_client.post("/recommend", json=payload)
        assert response.status_code == 422  # pydantic min_length validation

    async def test_missing_user_id_returns_422(self, async_client):
        payload = {k: v for k, v in BASE_PAYLOAD.items() if k != "userId"}
        response = await async_client.post("/recommend", json=payload)
        assert response.status_code == 422

    async def test_gemini_fallback_still_returns_200(self, async_client):
        """Even when Gemini fails, the service falls back to the top scored slot."""
        top_3 = [make_scored_slot("Ion", 9, 90.0)]
        with (
            patch("routers.recommend.rank_slots", return_value=top_3),
            patch(
                "routers.recommend.refine_with_gemini",
                return_value=(0, "Selected the highest-scored appointment slot based on rating, timing, and weather."),
            ),
        ):
            response = await async_client.post("/recommend", json=BASE_PAYLOAD)
        assert response.status_code == 200
        assert "highest-scored" in response.json()["reason"]

    async def test_second_gemini_choice_is_respected(self, async_client):
        top_3 = [
            make_scored_slot("First", 9, 90.0),
            make_scored_slot("Second", 14, 80.0),
        ]
        with (
            patch("routers.recommend.rank_slots", return_value=top_3),
            patch("routers.recommend.refine_with_gemini", return_value=(1, "Afternoon works better.")),
        ):
            response = await async_client.post("/recommend", json=BASE_PAYLOAD)
        data = response.json()
        assert data["recommended_slot"]["barber"] == "Second"
        assert data["recommended_slot"]["time"] == "14:00:00"

    async def test_score_breakdown_values_are_non_negative(self, async_client):
        top_3 = [make_scored_slot("Ion", 9, 85.0)]
        with (
            patch("routers.recommend.rank_slots", return_value=top_3),
            patch("routers.recommend.refine_with_gemini", return_value=(0, "Good!")),
        ):
            response = await async_client.post("/recommend", json=BASE_PAYLOAD)
        bd = response.json()["score_breakdown"]
        for key, val in bd.items():
            assert val >= 0.0, f"score_breakdown.{key} should be non-negative"

    async def test_with_calendar_flag(self, async_client):
        payload = {**BASE_PAYLOAD, "includeCalendar": True}
        top_3 = [make_scored_slot("Ion", 9, 85.0)]
        with (
            patch("routers.recommend.rank_slots", AsyncMock(return_value=top_3)),
            patch("routers.recommend.refine_with_gemini", return_value=(0, "Ok")),
        ):
            response = await async_client.post("/recommend", json=payload)
        assert response.status_code == 200
