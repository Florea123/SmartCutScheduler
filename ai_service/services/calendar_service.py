"""
Google Calendar integration.

Flow:
1. Frontend calls GET /calendar/connect?user_id=<id>  → returns OAuth URL
2. User authorises → Google redirects to GET /calendar/callback?code=...&state=<user_id>
3. Tokens are stored in TOKEN_STORAGE_PATH (JSON file, swap for Redis in production)
4. On each /recommend call with includeCalendar=true the service checks calendar events
"""

import asyncio
import json
import logging
from functools import partial
from pathlib import Path
from typing import Optional

from config import settings

logger = logging.getLogger(__name__)
SCOPES = ["https://www.googleapis.com/auth/calendar.readonly"]


# ---------------------------------------------------------------------------
# Token persistence (simple JSON file; swap for Redis / DB in production)
# ---------------------------------------------------------------------------

def _token_path() -> Path:
    path = Path(settings.token_storage_path)
    path.parent.mkdir(parents=True, exist_ok=True)
    return path


def _load_tokens() -> dict:
    path = _token_path()
    if path.exists():
        try:
            with open(path, "r", encoding="utf-8") as f:
                return json.load(f)
        except (json.JSONDecodeError, OSError):
            return {}
    return {}


def _save_tokens(tokens: dict) -> None:
    with open(_token_path(), "w", encoding="utf-8") as f:
        json.dump(tokens, f, indent=2)


# ---------------------------------------------------------------------------
# OAuth flow helpers
# ---------------------------------------------------------------------------

def _get_flow():  # type: ignore[return]
    """Build a google_auth_oauthlib Flow, or None if credentials are missing."""
    if not settings.google_client_id or not settings.google_client_secret:
        return None

    from google_auth_oauthlib.flow import Flow  # lazy import

    client_config = {
        "web": {
            "client_id": settings.google_client_id,
            "client_secret": settings.google_client_secret,
            "auth_uri": "https://accounts.google.com/o/oauth2/auth",
            "token_uri": "https://oauth2.googleapis.com/token",
            "redirect_uris": [settings.google_redirect_uri],
        }
    }
    flow = Flow.from_client_config(client_config, scopes=SCOPES)
    flow.redirect_uri = settings.google_redirect_uri
    return flow


def get_auth_url(user_id: str) -> Optional[str]:
    flow = _get_flow()
    if not flow:
        return None
    auth_url, _ = flow.authorization_url(
        access_type="offline",
        include_granted_scopes="true",
        state=user_id,
        prompt="consent",
    )
    return auth_url


def handle_oauth_callback(code: str, user_id: str) -> bool:
    flow = _get_flow()
    if not flow:
        return False
    try:
        flow.fetch_token(code=code)
        creds = flow.credentials
        tokens = _load_tokens()
        tokens[user_id] = {
            "token": creds.token,
            "refresh_token": creds.refresh_token,
            "token_uri": creds.token_uri,
            "client_id": creds.client_id,
            "client_secret": creds.client_secret,
            "scopes": list(creds.scopes or SCOPES),
        }
        _save_tokens(tokens)
        return True
    except Exception as exc:
        logger.error("OAuth callback error for user %s: %s", user_id, exc)
        return False


def is_calendar_connected(user_id: str) -> bool:
    return user_id in _load_tokens()


def disconnect_calendar(user_id: str) -> bool:
    tokens = _load_tokens()
    if user_id not in tokens:
        return False
    del tokens[user_id]
    _save_tokens(tokens)
    return True


# ---------------------------------------------------------------------------
# Calendar availability check
# ---------------------------------------------------------------------------

def _sync_check(creds, date: str, start_time: str, end_time: str) -> tuple[bool, str]:
    """Synchronous Google Calendar query (run in executor to avoid blocking)."""
    from googleapiclient.discovery import build
    from googleapiclient.errors import HttpError

    try:
        service = build("calendar", "v3", credentials=creds, cache_discovery=False)
        time_min = f"{date}T{start_time}+00:00"
        time_max = f"{date}T{end_time}+00:00"
        result = (
            service.events()
            .list(
                calendarId="primary",
                timeMin=time_min,
                timeMax=time_max,
                singleEvents=True,
                orderBy="startTime",
            )
            .execute()
        )
        events = result.get("items", [])
        if events:
            summary = events[0].get("summary", "event")
            return False, f"conflict with '{summary}'"
        return True, "available"
    except HttpError as exc:
        logger.warning("Google Calendar HttpError: %s", exc)
        return True, "calendar check failed"


async def check_slot_availability(
    user_id: str,
    date: str,
    start_time: str,
    end_time: str,
) -> tuple[bool, str]:
    """
    Return (is_available, reason).
    If the user hasn't connected their calendar → assume available.
    """
    from google.oauth2.credentials import Credentials  # lazy import

    tokens = _load_tokens()
    user_data = tokens.get(user_id)
    if not user_data:
        return True, "calendar not connected"

    creds = Credentials(
        token=user_data["token"],
        refresh_token=user_data.get("refresh_token"),
        token_uri=user_data["token_uri"],
        client_id=user_data["client_id"],
        client_secret=user_data["client_secret"],
        scopes=user_data.get("scopes", SCOPES),
    )

    try:
        loop = asyncio.get_event_loop()
        return await loop.run_in_executor(
            None, partial(_sync_check, creds, date, start_time, end_time)
        )
    except Exception as exc:
        logger.error("Calendar check error for user %s: %s", user_id, exc)
        return True, "calendar check failed"
