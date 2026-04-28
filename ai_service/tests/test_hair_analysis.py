"""
Unit tests for services/hair_analysis_service.py
"""

import json
from unittest.mock import AsyncMock, MagicMock, patch

import httpx
import pytest

from services.hair_analysis_service import (
    analyze_hair,
    build_recommendation_message,
    fetch_image_from_url,
)


# ---------------------------------------------------------------------------
# build_recommendation_message
# ---------------------------------------------------------------------------

class TestBuildRecommendationMessage:
    def test_moderate_without_weeks(self):
        msg = build_recommendation_message("moderate", None)
        assert "moment bun" in msg
        assert "săptămâni" not in msg

    def test_moderate_with_weeks(self):
        msg = build_recommendation_message("moderate", 5)
        assert "5 săptămâni" in msg
        assert "moment bun" in msg

    def test_significant_without_weeks(self):
        msg = build_recommendation_message("significant", None)
        assert "destul de mult" in msg

    def test_significant_with_weeks(self):
        msg = build_recommendation_message("significant", 8)
        assert "8 săptămâni" in msg

    def test_excessive_without_weeks(self):
        msg = build_recommendation_message("excessive", None)
        assert "foarte mult" in msg

    def test_excessive_with_weeks(self):
        msg = build_recommendation_message("excessive", 12)
        assert "12 săptămâni" in msg

    def test_unknown_level_returns_generic(self):
        msg = build_recommendation_message("unknown", None)
        assert "lua în considerare" in msg

    def test_none_level_returns_generic(self):
        msg = build_recommendation_message("none", None)
        assert "lua în considerare" in msg

    def test_minimal_level_returns_generic(self):
        msg = build_recommendation_message("minimal", 2)
        assert "lua în considerare" in msg
        assert "2 săptămâni" in msg

    def test_zero_weeks_not_shown(self):
        # estimated_weeks=0 is falsy — hint should not appear
        msg = build_recommendation_message("moderate", 0)
        assert "săptămâni" not in msg


# ---------------------------------------------------------------------------
# analyze_hair — no API key
# ---------------------------------------------------------------------------

@pytest.mark.asyncio
class TestAnalyzeHairNoApiKey:
    async def test_returns_fallback_when_no_api_key(self):
        with patch("services.hair_analysis_service.settings") as mock_settings:
            mock_settings.gemini_api_key = ""
            result = await analyze_hair(b"ref", b"cur")
        assert result["needs_haircut"] is False
        assert result["confidence"] == pytest.approx(0.0)
        assert result["hair_growth_level"] == "unknown"
        assert "configured" in result["reason"]


# ---------------------------------------------------------------------------
# analyze_hair — Gemini call paths
# ---------------------------------------------------------------------------

@pytest.mark.asyncio
class TestAnalyzeHairGeminiPaths:
    async def test_successful_analysis_returns_result(self):
        gemini_result = {
            "needs_haircut": True,
            "confidence": 0.9,
            "hair_growth_level": "significant",
            "reason": "Hair has grown a lot.",
            "estimated_weeks_since_haircut": 6,
        }
        with (
            patch("services.hair_analysis_service.settings") as mock_settings,
            patch("services.hair_analysis_service.asyncio.get_event_loop") as mock_loop,
        ):
            mock_settings.gemini_api_key = "fake-key"
            mock_settings.gemini_vision_model = "gemini-flash-lite-latest"
            loop = MagicMock()
            loop.run_in_executor = AsyncMock(return_value=gemini_result)
            mock_loop.return_value = loop

            result = await analyze_hair(b"ref-bytes", b"cur-bytes")

        assert result["needs_haircut"] is True
        assert result["confidence"] == pytest.approx(0.9)
        assert result["hair_growth_level"] == "significant"
        assert result["estimated_weeks_since_haircut"] == 6

    async def test_gemini_validation_error_propagated(self):
        gemini_error = {
            "error": True,
            "error_type": "no_person_reference",
            "error_message": "Poza de referință nu conține o persoană.",
        }
        with (
            patch("services.hair_analysis_service.settings") as mock_settings,
            patch("services.hair_analysis_service.asyncio.get_event_loop") as mock_loop,
        ):
            mock_settings.gemini_api_key = "fake-key"
            loop = MagicMock()
            loop.run_in_executor = AsyncMock(return_value=gemini_error)
            mock_loop.return_value = loop

            result = await analyze_hair(b"ref", b"cur")

        assert result["error"] is True
        assert result["error_type"] == "no_person_reference"

    async def test_json_decode_error_returns_fallback(self):
        with (
            patch("services.hair_analysis_service.settings") as mock_settings,
            patch("services.hair_analysis_service.asyncio.get_event_loop") as mock_loop,
        ):
            mock_settings.gemini_api_key = "fake-key"
            loop = MagicMock()
            loop.run_in_executor = AsyncMock(side_effect=json.JSONDecodeError("err", "", 0))
            mock_loop.return_value = loop

            result = await analyze_hair(b"ref", b"cur")

        assert result["needs_haircut"] is False
        assert "unexpected response" in result["reason"]

    async def test_generic_exception_returns_fallback(self):
        with (
            patch("services.hair_analysis_service.settings") as mock_settings,
            patch("services.hair_analysis_service.asyncio.get_event_loop") as mock_loop,
        ):
            mock_settings.gemini_api_key = "fake-key"
            loop = MagicMock()
            loop.run_in_executor = AsyncMock(side_effect=RuntimeError("network down"))
            mock_loop.return_value = loop

            result = await analyze_hair(b"ref", b"cur")

        assert result["needs_haircut"] is False
        assert "unavailable" in result["reason"]

    async def test_custom_mime_types_passed_through(self):
        gemini_result = {
            "needs_haircut": False,
            "confidence": 0.5,
            "hair_growth_level": "none",
            "reason": "No growth.",
            "estimated_weeks_since_haircut": None,
        }
        with (
            patch("services.hair_analysis_service.settings") as mock_settings,
            patch("services.hair_analysis_service.asyncio.get_event_loop") as mock_loop,
        ):
            mock_settings.gemini_api_key = "fake-key"
            loop = MagicMock()
            loop.run_in_executor = AsyncMock(return_value=gemini_result)
            mock_loop.return_value = loop

            result = await analyze_hair(b"ref", b"cur", "image/png", "image/webp")

        assert result["needs_haircut"] is False


# ---------------------------------------------------------------------------
# fetch_image_from_url
# ---------------------------------------------------------------------------

@pytest.mark.asyncio
class TestFetchImageFromUrl:
    async def test_successful_fetch_returns_bytes_and_mime(self):
        fake_content = b"fake-image-data"
        mock_response = MagicMock()
        mock_response.content = fake_content
        mock_response.headers = {"content-type": "image/jpeg"}
        mock_response.raise_for_status = MagicMock()

        with (
            patch("services.hair_analysis_service.settings") as mock_settings,
            patch("httpx.AsyncClient") as mock_client_cls,
        ):
            mock_settings.dotnet_api_url = "https://api:5000"
            mock_ctx = AsyncMock()
            mock_ctx.__aenter__ = AsyncMock(return_value=mock_ctx)
            mock_ctx.__aexit__ = AsyncMock(return_value=False)
            mock_ctx.get = AsyncMock(return_value=mock_response)
            mock_client_cls.return_value = mock_ctx

            data, mime = await fetch_image_from_url("https://example.com/photo.jpg")

        assert data == fake_content
        assert mime == "image/jpeg"

    async def test_relative_url_prepends_backend_base(self):
        fake_content = b"img"
        mock_response = MagicMock()
        mock_response.content = fake_content
        mock_response.headers = {"content-type": "image/png"}
        mock_response.raise_for_status = MagicMock()

        with (
            patch("services.hair_analysis_service.settings") as mock_settings,
            patch("httpx.AsyncClient") as mock_client_cls,
        ):
            mock_settings.dotnet_api_url = "https://api:5000"
            mock_ctx = AsyncMock()
            mock_ctx.__aenter__ = AsyncMock(return_value=mock_ctx)
            mock_ctx.__aexit__ = AsyncMock(return_value=False)
            mock_ctx.get = AsyncMock(return_value=mock_response)
            mock_client_cls.return_value = mock_ctx

            _, _ = await fetch_image_from_url("/profile-images/abc.jpg")

        # Verify the call URL started with the backend base
        called_url = mock_ctx.get.call_args[0][0]
        assert called_url.startswith("https://api:5000")
        assert "/profile-images/abc.jpg" in called_url

    async def test_http_status_error_raises(self):
        mock_response = MagicMock()
        mock_response.raise_for_status = MagicMock(
            side_effect=httpx.HTTPStatusError("404", request=MagicMock(), response=MagicMock())
        )

        with (
            patch("services.hair_analysis_service.settings") as mock_settings,
            patch("httpx.AsyncClient") as mock_client_cls,
        ):
            mock_settings.dotnet_api_url = "https://api:5000"
            mock_ctx = AsyncMock()
            mock_ctx.__aenter__ = AsyncMock(return_value=mock_ctx)
            mock_ctx.__aexit__ = AsyncMock(return_value=False)
            mock_ctx.get = AsyncMock(return_value=mock_response)
            mock_client_cls.return_value = mock_ctx

            with pytest.raises(httpx.HTTPStatusError):
                await fetch_image_from_url("https://example.com/bad.jpg")

    async def test_content_type_with_charset_stripped(self):
        fake_content = b"img"
        mock_response = MagicMock()
        mock_response.content = fake_content
        mock_response.headers = {"content-type": "image/jpeg; charset=utf-8"}
        mock_response.raise_for_status = MagicMock()

        with (
            patch("services.hair_analysis_service.settings") as mock_settings,
            patch("httpx.AsyncClient") as mock_client_cls,
        ):
            mock_settings.dotnet_api_url = "https://api:5000"
            mock_ctx = AsyncMock()
            mock_ctx.__aenter__ = AsyncMock(return_value=mock_ctx)
            mock_ctx.__aexit__ = AsyncMock(return_value=False)
            mock_ctx.get = AsyncMock(return_value=mock_response)
            mock_client_cls.return_value = mock_ctx

            _, mime = await fetch_image_from_url("https://example.com/photo.jpg")

        assert mime == "image/jpeg"
