"""
Unit tests for services/weather_service.py
"""

from unittest.mock import AsyncMock, MagicMock, patch

import httpx
import pytest

from services.weather_service import _condition_to_score, get_weather_score


# ---------------------------------------------------------------------------
# _condition_to_score
# ---------------------------------------------------------------------------

class TestConditionToScore:
    def test_thunderstorm_200(self):
        score, label = _condition_to_score(200)
        assert score == pytest.approx(2.0)
        assert "thunderstorm" in label

    def test_thunderstorm_232(self):
        score, _ = _condition_to_score(232)
        assert score == pytest.approx(2.0)

    def test_drizzle_300(self):
        score, label = _condition_to_score(300)
        assert score == pytest.approx(10.0)
        assert "drizzle" in label

    def test_rain_500(self):
        score, label = _condition_to_score(500)
        assert score == pytest.approx(5.0)
        assert "rain" in label

    def test_snow_600(self):
        score, label = _condition_to_score(600)
        assert score == pytest.approx(8.0)
        assert "snow" in label

    def test_fog_700(self):
        score, label = _condition_to_score(700)
        assert score == pytest.approx(12.0)
        assert "fog" in label or "mist" in label

    def test_clear_sky_800(self):
        score, label = _condition_to_score(800)
        assert score == pytest.approx(20.0)
        assert "clear" in label

    def test_few_clouds_801(self):
        score, _ = _condition_to_score(801)
        assert score == pytest.approx(18.0)

    def test_broken_clouds_803(self):
        score, _ = _condition_to_score(803)
        assert score == pytest.approx(14.0)

    def test_overcast_804(self):
        score, _ = _condition_to_score(804)
        assert score == pytest.approx(14.0)

    def test_unknown_condition_id_returns_neutral(self):
        score, label = _condition_to_score(9999)
        assert score == pytest.approx(14.0)
        assert label == "unknown"

    def test_score_within_range_for_known_ids(self):
        known_ids = [200, 300, 500, 600, 700, 800, 801, 803]
        for cid in known_ids:
            score, _ = _condition_to_score(cid)
            assert 0.0 <= score <= 20.0


# ---------------------------------------------------------------------------
# get_weather_score — no API key
# ---------------------------------------------------------------------------

@pytest.mark.asyncio
class TestGetWeatherScoreNoApiKey:
    async def test_returns_neutral_when_no_api_key(self):
        with patch("services.weather_service.settings") as mock_settings:
            mock_settings.weather_api_key = ""
            score, label = await get_weather_score("Bucharest")
        assert score == pytest.approx(14.0)
        assert "unavailable" in label

    async def test_neutral_score_is_within_range(self):
        with patch("services.weather_service.settings") as mock_settings:
            mock_settings.weather_api_key = ""
            score, _ = await get_weather_score("Bucharest")
        assert 0.0 <= score <= 20.0


# ---------------------------------------------------------------------------
# get_weather_score — successful API call
# ---------------------------------------------------------------------------

@pytest.mark.asyncio
class TestGetWeatherScoreSuccess:
    async def _call_with_condition(self, condition_id: int):
        mock_response = MagicMock()
        mock_response.json.return_value = {"weather": [{"id": condition_id}]}
        mock_response.raise_for_status = MagicMock()

        with (
            patch("services.weather_service.settings") as mock_settings,
            patch("httpx.AsyncClient") as mock_client_cls,
        ):
            mock_settings.weather_api_key = "fake-key"
            mock_settings.weather_api_url = "https://api.openweathermap.org/data/2.5"
            mock_ctx = AsyncMock()
            mock_ctx.__aenter__ = AsyncMock(return_value=mock_ctx)
            mock_ctx.__aexit__ = AsyncMock(return_value=False)
            mock_ctx.get = AsyncMock(return_value=mock_response)
            mock_client_cls.return_value = mock_ctx

            return await get_weather_score("Bucharest")

    async def test_clear_sky_returns_20(self):
        score, label = await self._call_with_condition(800)
        assert score == pytest.approx(20.0)
        assert "clear" in label

    async def test_rain_returns_5(self):
        score, _ = await self._call_with_condition(500)
        assert score == pytest.approx(5.0)

    async def test_thunderstorm_returns_2(self):
        score, _ = await self._call_with_condition(200)
        assert score == pytest.approx(2.0)

    async def test_snow_returns_8(self):
        score, _ = await self._call_with_condition(600)
        assert score == pytest.approx(8.0)


# ---------------------------------------------------------------------------
# get_weather_score — error paths
# ---------------------------------------------------------------------------

@pytest.mark.asyncio
class TestGetWeatherScoreErrors:
    async def test_timeout_returns_neutral(self):
        with (
            patch("services.weather_service.settings") as mock_settings,
            patch("httpx.AsyncClient") as mock_client_cls,
        ):
            mock_settings.weather_api_key = "fake-key"
            mock_settings.weather_api_url = "https://api.openweathermap.org/data/2.5"
            mock_ctx = AsyncMock()
            mock_ctx.__aenter__ = AsyncMock(return_value=mock_ctx)
            mock_ctx.__aexit__ = AsyncMock(return_value=False)
            mock_ctx.get = AsyncMock(side_effect=httpx.TimeoutException("timeout"))
            mock_client_cls.return_value = mock_ctx

            score, label = await get_weather_score("Bucharest")

        assert score == pytest.approx(14.0)
        assert "unavailable" in label

    async def test_http_status_error_returns_neutral(self):
        mock_resp = httpx.Response(401)
        exc = httpx.HTTPStatusError(
            "Unauthorized",
            request=httpx.Request("GET", "https://x"),
            response=mock_resp,
        )
        with (
            patch("services.weather_service.settings") as mock_settings,
            patch("httpx.AsyncClient") as mock_client_cls,
        ):
            mock_settings.weather_api_key = "fake-key"
            mock_settings.weather_api_url = "https://api.openweathermap.org/data/2.5"
            mock_ctx = AsyncMock()
            mock_ctx.__aenter__ = AsyncMock(return_value=mock_ctx)
            mock_ctx.__aexit__ = AsyncMock(return_value=False)
            mock_ctx.get = AsyncMock(side_effect=exc)
            mock_client_cls.return_value = mock_ctx

            score, label = await get_weather_score("Bucharest")

        assert score == pytest.approx(14.0)
        assert "unavailable" in label

    async def test_generic_exception_returns_neutral(self):
        with (
            patch("services.weather_service.settings") as mock_settings,
            patch("httpx.AsyncClient") as mock_client_cls,
        ):
            mock_settings.weather_api_key = "fake-key"
            mock_settings.weather_api_url = "https://api.openweathermap.org/data/2.5"
            mock_ctx = AsyncMock()
            mock_ctx.__aenter__ = AsyncMock(return_value=mock_ctx)
            mock_ctx.__aexit__ = AsyncMock(return_value=False)
            mock_ctx.get = AsyncMock(side_effect=RuntimeError("unexpected"))
            mock_client_cls.return_value = mock_ctx

            score, label = await get_weather_score("Bucharest")

        assert score == pytest.approx(14.0)
        assert "unavailable" in label
