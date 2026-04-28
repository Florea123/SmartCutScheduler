import logging
from typing import Annotated

from fastapi import APIRouter, HTTPException, Query
from fastapi.responses import RedirectResponse

from config import settings
from models.schemas import CalendarConnectResponse, CalendarStatusResponse
from services import calendar_service

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/calendar", tags=["calendar"])


@router.get(
    "/connect",
    summary="Get Google OAuth URL to connect user's calendar",
    responses={
        501: {"description": "Google Calendar integration not configured or OAuth URL build failed"},
    },
)
async def connect_calendar(
    user_id: Annotated[str, Query(description="User ID to associate the calendar with")],
) -> CalendarConnectResponse:
    if not settings.google_client_id or not settings.google_client_secret:
        raise HTTPException(
            status_code=501,
            detail="Google Calendar integration is not configured on this server.",
        )
    auth_url = calendar_service.get_auth_url(user_id)
    if not auth_url:
        raise HTTPException(status_code=501, detail="Failed to build Google OAuth URL.")
    return CalendarConnectResponse(auth_url=auth_url)


@router.get(
    "/callback",
    include_in_schema=False,
    responses={
        400: {"description": "OAuth callback failed"},
    },
)
async def oauth_callback(
    code: Annotated[str, Query()],
    state: Annotated[str, Query(description="user_id is passed as OAuth state")],
):
    """
    Google redirects here after the user authorises access.
    The OAuth *state* parameter carries the user ID.
    On success the user is redirected to the frontend /settings page.
    """
    success = calendar_service.handle_oauth_callback(code=code, user_id=state)
    if not success:
        raise HTTPException(status_code=400, detail="OAuth callback failed. Please try again.")
    return RedirectResponse(url="http://localhost:5177/settings?calendar=connected")


@router.get(
    "/status",
    summary="Check if a user has connected their Google Calendar",
)
async def calendar_status(
    user_id: Annotated[str, Query()],
) -> CalendarStatusResponse:
    connected = calendar_service.is_calendar_connected(user_id)
    return CalendarStatusResponse(connected=connected)


@router.delete(
    "/disconnect",
    summary="Disconnect a user's Google Calendar",
    responses={
        404: {"description": "No calendar connected for this user"},
    },
)
async def disconnect_calendar(user_id: Annotated[str, Query()]) -> dict:
    removed = calendar_service.disconnect_calendar(user_id)
    if not removed:
        raise HTTPException(status_code=404, detail="No calendar connected for this user.")
    return {"message": "Google Calendar disconnected successfully."}
