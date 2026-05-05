"""
Integration tests for the /haircut/analyze endpoint.
External services (analyze_hair, fetch_image_from_url) are mocked.
"""

import io
from unittest.mock import AsyncMock, patch

import pytest
from httpx import ASGITransport, AsyncClient

from main import app


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _make_upload_file(content: bytes = b"fake-image", content_type: str = "image/jpeg"):
    return ("files", (content_type.split("/")[1] + ".jpg", io.BytesIO(content), content_type))


_GOOD_ANALYSIS = {
    "needs_haircut": True,
    "confidence": 0.88,
    "hair_growth_level": "significant",
    "reason": "Hair has grown considerably.",
    "estimated_weeks_since_haircut": 6,
}

_NO_HAIRCUT_ANALYSIS = {
    "needs_haircut": False,
    "confidence": 0.95,
    "hair_growth_level": "none",
    "reason": "Hair looks fresh.",
    "estimated_weeks_since_haircut": 1,
}

_VALIDATION_ERROR = {
    "error": True,
    "error_type": "no_person_reference",
    "error_message": "Poza de referință nu conține o persoană.",
}


@pytest.fixture
async def async_client():
    async with AsyncClient(transport=ASGITransport(app=app), base_url="https://test") as client:
        yield client


# ---------------------------------------------------------------------------
# Input validation tests
# ---------------------------------------------------------------------------

@pytest.mark.asyncio
class TestHaircutAnalyzeValidation:
    async def test_missing_both_reference_returns_400(self, async_client):
        """Neither reference_photo_url nor reference_photo provided."""
        files = {"current_photo": ("current.jpg", io.BytesIO(b"img"), "image/jpeg")}
        response = await async_client.post("/haircut/analyze", files=files)
        assert response.status_code == 400
        assert "reference" in response.json()["detail"].lower()

    async def test_unsupported_current_mime_returns_415(self, async_client):
        files = {
            "current_photo": ("current.gif", io.BytesIO(b"gif-data"), "image/gif"),
            "reference_photo": ("ref.jpg", io.BytesIO(b"ref-data"), "image/jpeg"),
        }
        response = await async_client.post("/haircut/analyze", files=files)
        assert response.status_code == 415

    async def test_unsupported_reference_mime_returns_415(self, async_client):
        files = {
            "current_photo": ("current.jpg", io.BytesIO(b"jpg-data"), "image/jpeg"),
            "reference_photo": ("ref.bmp", io.BytesIO(b"bmp-data"), "image/bmp"),
        }
        response = await async_client.post("/haircut/analyze", files=files)
        assert response.status_code == 415

    async def test_empty_current_photo_returns_400(self, async_client):
        files = {
            "current_photo": ("current.jpg", io.BytesIO(b""), "image/jpeg"),
            "reference_photo": ("ref.jpg", io.BytesIO(b"ref-data"), "image/jpeg"),
        }
        response = await async_client.post("/haircut/analyze", files=files)
        assert response.status_code == 400

    async def test_empty_reference_photo_returns_400(self, async_client):
        files = {
            "current_photo": ("current.jpg", io.BytesIO(b"cur-data"), "image/jpeg"),
            "reference_photo": ("ref.jpg", io.BytesIO(b""), "image/jpeg"),
        }
        response = await async_client.post("/haircut/analyze", files=files)
        assert response.status_code == 400

    async def test_oversized_current_photo_returns_413(self, async_client):
        big = b"x" * (10 * 1024 * 1024 + 1)
        files = {
            "current_photo": ("current.jpg", io.BytesIO(big), "image/jpeg"),
            "reference_photo": ("ref.jpg", io.BytesIO(b"ref-data"), "image/jpeg"),
        }
        response = await async_client.post("/haircut/analyze", files=files)
        assert response.status_code == 413


# ---------------------------------------------------------------------------
# Successful analysis via uploaded reference photo
# ---------------------------------------------------------------------------

@pytest.mark.asyncio
class TestHaircutAnalyzeWithUpload:
    async def test_needs_haircut_true_returns_200_with_message(self, async_client):
        with patch("routers.haircut.analyze_hair", AsyncMock(return_value=_GOOD_ANALYSIS)):
            files = {
                "current_photo": ("cur.jpg", io.BytesIO(b"cur"), "image/jpeg"),
                "reference_photo": ("ref.jpg", io.BytesIO(b"ref"), "image/jpeg"),
            }
            response = await async_client.post("/haircut/analyze", files=files)

        assert response.status_code == 200
        data = response.json()
        assert data["needs_haircut"] is True
        assert data["confidence"] == pytest.approx(0.88)
        assert data["hair_growth_level"] == "significant"
        assert data["recommendation_message"] is not None
        assert data["error"] is False

    async def test_needs_haircut_false_no_recommendation_message(self, async_client):
        with patch("routers.haircut.analyze_hair", AsyncMock(return_value=_NO_HAIRCUT_ANALYSIS)):
            files = {
                "current_photo": ("cur.jpg", io.BytesIO(b"cur"), "image/jpeg"),
                "reference_photo": ("ref.jpg", io.BytesIO(b"ref"), "image/jpeg"),
            }
            response = await async_client.post("/haircut/analyze", files=files)

        assert response.status_code == 200
        data = response.json()
        assert data["needs_haircut"] is False
        assert data["recommendation_message"] is None

    async def test_gemini_validation_error_propagated(self, async_client):
        with patch("routers.haircut.analyze_hair", AsyncMock(return_value=_VALIDATION_ERROR)):
            files = {
                "current_photo": ("cur.jpg", io.BytesIO(b"cur"), "image/jpeg"),
                "reference_photo": ("ref.jpg", io.BytesIO(b"ref"), "image/jpeg"),
            }
            response = await async_client.post("/haircut/analyze", files=files)

        assert response.status_code == 200
        data = response.json()
        assert data["error"] is True
        assert data["error_type"] == "no_person_reference"
        assert data["error_message"] is not None

    async def test_png_upload_accepted(self, async_client):
        with patch("routers.haircut.analyze_hair", AsyncMock(return_value=_NO_HAIRCUT_ANALYSIS)):
            files = {
                "current_photo": ("cur.png", io.BytesIO(b"cur"), "image/png"),
                "reference_photo": ("ref.png", io.BytesIO(b"ref"), "image/png"),
            }
            response = await async_client.post("/haircut/analyze", files=files)

        assert response.status_code == 200

    async def test_webp_upload_accepted(self, async_client):
        with patch("routers.haircut.analyze_hair", AsyncMock(return_value=_NO_HAIRCUT_ANALYSIS)):
            files = {
                "current_photo": ("cur.webp", io.BytesIO(b"cur"), "image/webp"),
                "reference_photo": ("ref.webp", io.BytesIO(b"ref"), "image/webp"),
            }
            response = await async_client.post("/haircut/analyze", files=files)

        assert response.status_code == 200

    async def test_response_has_all_fields(self, async_client):
        with patch("routers.haircut.analyze_hair", AsyncMock(return_value=_GOOD_ANALYSIS)):
            files = {
                "current_photo": ("cur.jpg", io.BytesIO(b"cur"), "image/jpeg"),
                "reference_photo": ("ref.jpg", io.BytesIO(b"ref"), "image/jpeg"),
            }
            response = await async_client.post("/haircut/analyze", files=files)

        data = response.json()
        for field in ["error", "needs_haircut", "confidence", "hair_growth_level", "reason"]:
            assert field in data, f"Missing field: {field}"


# ---------------------------------------------------------------------------
# Reference photo via URL
# ---------------------------------------------------------------------------

@pytest.mark.asyncio
class TestHaircutAnalyzeWithUrl:
    async def test_reference_url_fetched_and_analyzed(self, async_client):
        with (
            patch("routers.haircut.fetch_image_from_url", AsyncMock(return_value=(b"ref-bytes", "image/jpeg"))),
            patch("routers.haircut.analyze_hair", AsyncMock(return_value=_GOOD_ANALYSIS)),
        ):
            files = {"current_photo": ("cur.jpg", io.BytesIO(b"cur"), "image/jpeg")}
            data = {"reference_photo_url": "https://backend/photo.jpg"}
            response = await async_client.post("/haircut/analyze", files=files, data=data)

        assert response.status_code == 200
        assert response.json()["needs_haircut"] is True

    async def test_reference_url_http_error_returns_502(self, async_client):
        import httpx as _httpx

        mock_resp = _httpx.Response(404)
        exc = _httpx.HTTPStatusError("not found", request=_httpx.Request("GET", "https://x"), response=mock_resp)

        with patch("routers.haircut.fetch_image_from_url", AsyncMock(side_effect=exc)):
            files = {"current_photo": ("cur.jpg", io.BytesIO(b"cur"), "image/jpeg")}
            data = {"reference_photo_url": "https://backend/missing.jpg"}
            response = await async_client.post("/haircut/analyze", files=files, data=data)

        assert response.status_code == 502
        assert "reference photo" in response.json()["detail"].lower()

    async def test_reference_url_generic_error_returns_502(self, async_client):
        with patch("routers.haircut.fetch_image_from_url", AsyncMock(side_effect=Exception("conn refused"))):
            files = {"current_photo": ("cur.jpg", io.BytesIO(b"cur"), "image/jpeg")}
            data = {"reference_photo_url": "https://backend/bad.jpg"}
            response = await async_client.post("/haircut/analyze", files=files, data=data)

        assert response.status_code == 502

    async def test_invalid_weeks_string_falls_back_to_none(self, async_client):
        """Gemini returnează un string invalid pentru estimated_weeks_since_haircut -> None."""
        bad_analysis = {**_GOOD_ANALYSIS, "estimated_weeks_since_haircut": "invalid-value!!"}
        with patch("routers.haircut.analyze_hair", AsyncMock(return_value=bad_analysis)):
            files = {
                "current_photo": ("cur.jpg", io.BytesIO(b"cur"), "image/jpeg"),
                "reference_photo": ("ref.jpg", io.BytesIO(b"ref"), "image/jpeg"),
            }
            response = await async_client.post("/haircut/analyze", files=files)

        assert response.status_code == 200
        assert response.json()["estimated_weeks_since_haircut"] is None
