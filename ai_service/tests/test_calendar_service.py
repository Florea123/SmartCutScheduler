"""Tests for services/calendar_service.py"""

import json
import pytest
from pathlib import Path
from unittest.mock import patch, MagicMock, mock_open, AsyncMock


# ---------------------------------------------------------------------------
# _token_path
# ---------------------------------------------------------------------------

class TestTokenPath:
    def test_returns_path_object(self, tmp_path):
        from services.calendar_service import _token_path
        with patch("services.calendar_service.settings") as mock_settings:
            mock_settings.token_storage_path = str(tmp_path / "test_tokens.json")
            path = _token_path()
        assert isinstance(path, Path)


# ---------------------------------------------------------------------------
# _load_tokens
# ---------------------------------------------------------------------------

class TestLoadTokens:
    def test_returns_empty_dict_when_file_missing(self, tmp_path):
        from services.calendar_service import _load_tokens
        with patch("services.calendar_service.settings") as mock_settings:
            mock_settings.token_storage_path = str(tmp_path / "nonexistent.json")
            result = _load_tokens()
        assert result == {}

    def test_returns_tokens_when_file_exists(self, tmp_path):
        from services.calendar_service import _load_tokens
        token_file = tmp_path / "tokens.json"
        token_file.write_text(json.dumps({"user1": {"token": "abc"}}))
        with patch("services.calendar_service.settings") as mock_settings:
            mock_settings.token_storage_path = str(token_file)
            result = _load_tokens()
        assert result == {"user1": {"token": "abc"}}

    def test_returns_empty_dict_on_invalid_json(self, tmp_path):
        from services.calendar_service import _load_tokens
        token_file = tmp_path / "tokens.json"
        token_file.write_text("invalid json!!!")
        with patch("services.calendar_service.settings") as mock_settings:
            mock_settings.token_storage_path = str(token_file)
            result = _load_tokens()
        assert result == {}


# ---------------------------------------------------------------------------
# _save_tokens
# ---------------------------------------------------------------------------

class TestSaveTokens:
    def test_saves_tokens_to_file(self, tmp_path):
        from services.calendar_service import _save_tokens
        token_file = tmp_path / "tokens.json"
        with patch("services.calendar_service.settings") as mock_settings:
            mock_settings.token_storage_path = str(token_file)
            _save_tokens({"user1": {"token": "xyz"}})
        data = json.loads(token_file.read_text())
        assert data == {"user1": {"token": "xyz"}}


# ---------------------------------------------------------------------------
# _get_flow
# ---------------------------------------------------------------------------

class TestGetFlow:
    def test_returns_none_when_credentials_missing(self):
        from services.calendar_service import _get_flow
        with patch("services.calendar_service.settings") as mock_settings:
            mock_settings.google_client_id = ""
            mock_settings.google_client_secret = ""
            result = _get_flow()
        assert result is None

    def test_returns_flow_when_credentials_present(self):
        from services.calendar_service import _get_flow
        mock_flow = MagicMock()
        with patch("services.calendar_service.settings") as mock_settings:
            mock_settings.google_client_id = "client_id"
            mock_settings.google_client_secret = "client_secret"
            mock_settings.google_redirect_uri = "https://localhost/callback"
            with patch("google_auth_oauthlib.flow.Flow.from_client_config", return_value=mock_flow):
                result = _get_flow()
        assert result is mock_flow


# ---------------------------------------------------------------------------
# get_auth_url
# ---------------------------------------------------------------------------

class TestGetAuthUrl:
    def test_returns_none_when_flow_is_none(self):
        from services.calendar_service import get_auth_url
        with patch("services.calendar_service._get_flow", return_value=None):
            result = get_auth_url("user123")
        assert result is None

    def test_returns_auth_url_when_flow_present(self):
        from services.calendar_service import get_auth_url
        mock_flow = MagicMock()
        mock_flow.authorization_url.return_value = ("https://auth.url", "state")
        with patch("services.calendar_service._get_flow", return_value=mock_flow):
            result = get_auth_url("user123")
        assert result == "https://auth.url"


# ---------------------------------------------------------------------------
# handle_oauth_callback
# ---------------------------------------------------------------------------

class TestHandleOAuthCallback:
    def test_returns_false_when_flow_is_none(self):
        from services.calendar_service import handle_oauth_callback
        with patch("services.calendar_service._get_flow", return_value=None):
            result = handle_oauth_callback("code", "user123")
        assert result is False

    def test_returns_true_on_successful_callback(self, tmp_path):
        from services.calendar_service import handle_oauth_callback
        mock_creds = MagicMock()
        mock_creds.token = "token_val"
        mock_creds.refresh_token = "refresh_val"
        mock_creds.token_uri = "https://token.uri"
        mock_creds.client_id = "client_id"
        mock_creds.client_secret = "client_secret"
        mock_creds.scopes = ["https://www.googleapis.com/auth/calendar.readonly"]
        mock_flow = MagicMock()
        mock_flow.credentials = mock_creds

        with patch("services.calendar_service._get_flow", return_value=mock_flow), \
             patch("services.calendar_service.settings") as mock_settings:
            mock_settings.token_storage_path = str(tmp_path / "tokens.json")
            result = handle_oauth_callback("auth_code", "user1")
        assert result is True

    def test_returns_false_on_exception(self):
        from services.calendar_service import handle_oauth_callback
        mock_flow = MagicMock()
        mock_flow.fetch_token.side_effect = Exception("OAuth error")

        with patch("services.calendar_service._get_flow", return_value=mock_flow):
            result = handle_oauth_callback("bad_code", "user1")
        assert result is False


# ---------------------------------------------------------------------------
# is_calendar_connected
# ---------------------------------------------------------------------------

class TestIsCalendarConnected:
    def test_returns_false_when_user_not_in_tokens(self, tmp_path):
        from services.calendar_service import is_calendar_connected
        token_file = tmp_path / "tokens.json"
        token_file.write_text("{}")
        with patch("services.calendar_service.settings") as mock_settings:
            mock_settings.token_storage_path = str(token_file)
            result = is_calendar_connected("user999")
        assert result is False

    def test_returns_true_when_user_in_tokens(self, tmp_path):
        from services.calendar_service import is_calendar_connected
        token_file = tmp_path / "tokens.json"
        token_file.write_text(json.dumps({"user1": {"token": "x"}}))
        with patch("services.calendar_service.settings") as mock_settings:
            mock_settings.token_storage_path = str(token_file)
            result = is_calendar_connected("user1")
        assert result is True


# ---------------------------------------------------------------------------
# disconnect_calendar
# ---------------------------------------------------------------------------

class TestDisconnectCalendar:
    def test_returns_false_when_user_not_found(self, tmp_path):
        from services.calendar_service import disconnect_calendar
        token_file = tmp_path / "tokens.json"
        token_file.write_text("{}")
        with patch("services.calendar_service.settings") as mock_settings:
            mock_settings.token_storage_path = str(token_file)
            result = disconnect_calendar("nonexistent")
        assert result is False

    def test_returns_true_and_removes_user(self, tmp_path):
        from services.calendar_service import disconnect_calendar
        token_file = tmp_path / "tokens.json"
        token_file.write_text(json.dumps({"user1": {"token": "x"}, "user2": {"token": "y"}}))
        with patch("services.calendar_service.settings") as mock_settings:
            mock_settings.token_storage_path = str(token_file)
            result = disconnect_calendar("user1")
        assert result is True
        remaining = json.loads(token_file.read_text())
        assert "user1" not in remaining
        assert "user2" in remaining


# ---------------------------------------------------------------------------
# check_slot_availability
# ---------------------------------------------------------------------------

class TestCheckSlotAvailability:
    @pytest.mark.asyncio
    async def test_returns_available_when_not_connected(self, tmp_path):
        from services.calendar_service import check_slot_availability
        token_file = tmp_path / "tokens.json"
        token_file.write_text("{}")
        with patch("services.calendar_service.settings") as mock_settings:
            mock_settings.token_storage_path = str(token_file)
            is_avail, reason = await check_slot_availability("user1", "2024-03-18", "10:00:00", "10:30:00")
        assert is_avail is True
        assert "not connected" in reason

    @pytest.mark.asyncio
    async def test_returns_available_on_executor_exception(self, tmp_path):
        from services.calendar_service import check_slot_availability
        token_file = tmp_path / "tokens.json"
        token_file.write_text(json.dumps({"user1": {
            "token": "t", "refresh_token": "r",
            "token_uri": "https://token.uri",
            "client_id": "cid", "client_secret": "csec",
            "scopes": ["https://www.googleapis.com/auth/calendar.readonly"]
        }}))

        with patch("services.calendar_service.settings") as mock_settings, \
             patch("google.oauth2.credentials.Credentials"), \
             patch("asyncio.get_event_loop") as mock_loop:
            mock_settings.token_storage_path = str(token_file)
            mock_loop.return_value.run_in_executor = AsyncMock(side_effect=Exception("loop error"))
            is_avail, reason = await check_slot_availability("user1", "2024-03-18", "10:00:00", "10:30:00")
        assert is_avail is True
        assert "failed" in reason
