"""
Unit tests for services/dotnet_client.py
"""

from unittest.mock import AsyncMock, MagicMock, patch

import httpx
import pytest

from services.dotnet_client import DotNetClient


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _make_client(auth_token=None) -> DotNetClient:
    with patch("services.dotnet_client.settings") as mock_settings:
        mock_settings.dotnet_api_url = "https://api:5000"
        mock_settings.dotnet_api_timeout = 10
        client = DotNetClient(auth_token=auth_token)
    client._base_url = "https://api:5000"
    client._timeout = 10
    return client


# ---------------------------------------------------------------------------
# DotNetClient initialization
# ---------------------------------------------------------------------------

class TestDotNetClientInit:
    def test_no_auth_token_no_authorization_header(self):
        with patch("services.dotnet_client.settings") as mock_settings:
            mock_settings.dotnet_api_url = "https://api:5000"
            mock_settings.dotnet_api_timeout = 10
            client = DotNetClient()
        assert "Authorization" not in client._headers

    def test_auth_token_sets_bearer_header(self):
        with patch("services.dotnet_client.settings") as mock_settings:
            mock_settings.dotnet_api_url = "https://api:5000"
            mock_settings.dotnet_api_timeout = 10
            client = DotNetClient(auth_token="my-token")
        assert client._headers.get("Authorization") == "Bearer my-token"

    def test_trailing_slash_stripped_from_base_url(self):
        with patch("services.dotnet_client.settings") as mock_settings:
            mock_settings.dotnet_api_url = "https://api:5000/"
            mock_settings.dotnet_api_timeout = 10
            client = DotNetClient()
        assert not client._base_url.endswith("/")


# ---------------------------------------------------------------------------
# get_barber_reviews
# ---------------------------------------------------------------------------

@pytest.mark.asyncio
class TestGetBarberReviews:
    async def test_successful_returns_reviews(self):
        reviews = [{"rating": 5, "comment": "Great!"}]
        mock_response = MagicMock()
        mock_response.json.return_value = reviews
        mock_response.raise_for_status = MagicMock()

        with patch("httpx.AsyncClient") as mock_client_cls:
            mock_ctx = AsyncMock()
            mock_ctx.__aenter__ = AsyncMock(return_value=mock_ctx)
            mock_ctx.__aexit__ = AsyncMock(return_value=False)
            mock_ctx.get = AsyncMock(return_value=mock_response)
            mock_client_cls.return_value = mock_ctx

            client = _make_client()
            result = await client.get_barber_reviews("barber-123")

        assert result == reviews

    async def test_timeout_returns_empty_list(self):
        with patch("httpx.AsyncClient") as mock_client_cls:
            mock_ctx = AsyncMock()
            mock_ctx.__aenter__ = AsyncMock(return_value=mock_ctx)
            mock_ctx.__aexit__ = AsyncMock(return_value=False)
            mock_ctx.get = AsyncMock(side_effect=httpx.TimeoutException("timeout"))
            mock_client_cls.return_value = mock_ctx

            client = _make_client()
            result = await client.get_barber_reviews("barber-123")

        assert result == []

    async def test_http_status_error_returns_empty_list(self):
        mock_resp = httpx.Response(404)
        exc = httpx.HTTPStatusError("not found", request=httpx.Request("GET", "https://x"), response=mock_resp)

        with patch("httpx.AsyncClient") as mock_client_cls:
            mock_ctx = AsyncMock()
            mock_ctx.__aenter__ = AsyncMock(return_value=mock_ctx)
            mock_ctx.__aexit__ = AsyncMock(return_value=False)
            mock_ctx.get = AsyncMock(side_effect=exc)
            mock_client_cls.return_value = mock_ctx

            client = _make_client()
            result = await client.get_barber_reviews("barber-123")

        assert result == []

    async def test_generic_exception_returns_empty_list(self):
        with patch("httpx.AsyncClient") as mock_client_cls:
            mock_ctx = AsyncMock()
            mock_ctx.__aenter__ = AsyncMock(return_value=mock_ctx)
            mock_ctx.__aexit__ = AsyncMock(return_value=False)
            mock_ctx.get = AsyncMock(side_effect=RuntimeError("unexpected"))
            mock_client_cls.return_value = mock_ctx

            client = _make_client()
            result = await client.get_barber_reviews("barber-123")

        assert result == []

    async def test_auth_header_forwarded(self):
        mock_response = MagicMock()
        mock_response.json.return_value = []
        mock_response.raise_for_status = MagicMock()

        with patch("httpx.AsyncClient") as mock_client_cls:
            mock_ctx = AsyncMock()
            mock_ctx.__aenter__ = AsyncMock(return_value=mock_ctx)
            mock_ctx.__aexit__ = AsyncMock(return_value=False)
            mock_ctx.get = AsyncMock(return_value=mock_response)
            mock_client_cls.return_value = mock_ctx

            client = _make_client(auth_token="token-xyz")
            await client.get_barber_reviews("b1")

        _, kwargs = mock_ctx.get.call_args
        headers = kwargs.get("headers", {})
        assert headers.get("Authorization") == "Bearer token-xyz"


# ---------------------------------------------------------------------------
# get_user_appointments
# ---------------------------------------------------------------------------

@pytest.mark.asyncio
class TestGetUserAppointments:
    async def test_successful_returns_appointments(self):
        appointments = [{"id": "appt-1", "startTime": "10:00:00"}]
        mock_response = MagicMock()
        mock_response.json.return_value = appointments
        mock_response.raise_for_status = MagicMock()

        with patch("httpx.AsyncClient") as mock_client_cls:
            mock_ctx = AsyncMock()
            mock_ctx.__aenter__ = AsyncMock(return_value=mock_ctx)
            mock_ctx.__aexit__ = AsyncMock(return_value=False)
            mock_ctx.get = AsyncMock(return_value=mock_response)
            mock_client_cls.return_value = mock_ctx

            client = _make_client(auth_token="user-token")
            result = await client.get_user_appointments()

        assert result == appointments

    async def test_timeout_returns_empty_list(self):
        with patch("httpx.AsyncClient") as mock_client_cls:
            mock_ctx = AsyncMock()
            mock_ctx.__aenter__ = AsyncMock(return_value=mock_ctx)
            mock_ctx.__aexit__ = AsyncMock(return_value=False)
            mock_ctx.get = AsyncMock(side_effect=httpx.TimeoutException("timeout"))
            mock_client_cls.return_value = mock_ctx

            client = _make_client()
            result = await client.get_user_appointments()

        assert result == []

    async def test_http_status_error_returns_empty_list(self):
        mock_resp = httpx.Response(401)
        exc = httpx.HTTPStatusError("unauthorized", request=httpx.Request("GET", "https://x"), response=mock_resp)

        with patch("httpx.AsyncClient") as mock_client_cls:
            mock_ctx = AsyncMock()
            mock_ctx.__aenter__ = AsyncMock(return_value=mock_ctx)
            mock_ctx.__aexit__ = AsyncMock(return_value=False)
            mock_ctx.get = AsyncMock(side_effect=exc)
            mock_client_cls.return_value = mock_ctx

            client = _make_client()
            result = await client.get_user_appointments()

        assert result == []

    async def test_generic_exception_returns_empty_list(self):
        with patch("httpx.AsyncClient") as mock_client_cls:
            mock_ctx = AsyncMock()
            mock_ctx.__aenter__ = AsyncMock(return_value=mock_ctx)
            mock_ctx.__aexit__ = AsyncMock(return_value=False)
            mock_ctx.get = AsyncMock(side_effect=RuntimeError("conn error"))
            mock_client_cls.return_value = mock_ctx

            client = _make_client()
            result = await client.get_user_appointments()

        assert result == []

    async def test_hits_correct_endpoint(self):
        mock_response = MagicMock()
        mock_response.json.return_value = []
        mock_response.raise_for_status = MagicMock()

        with patch("httpx.AsyncClient") as mock_client_cls:
            mock_ctx = AsyncMock()
            mock_ctx.__aenter__ = AsyncMock(return_value=mock_ctx)
            mock_ctx.__aexit__ = AsyncMock(return_value=False)
            mock_ctx.get = AsyncMock(return_value=mock_response)
            mock_client_cls.return_value = mock_ctx

            client = _make_client()
            await client.get_user_appointments()

        called_url = mock_ctx.get.call_args[0][0]
        assert "/api/appointments/my" in called_url
