import logging
from typing import Optional

import httpx

from config import settings

logger = logging.getLogger(__name__)


class DotNetClient:
    """HTTP client for communicating with the .NET SmartCutScheduler API."""

    def __init__(self, auth_token: Optional[str] = None) -> None:
        self._base_url = settings.dotnet_api_url.rstrip("/")
        self._timeout = settings.dotnet_api_timeout
        self._headers: dict[str, str] = {}
        if auth_token:
            self._headers["Authorization"] = f"Bearer {auth_token}"

    async def get_barber_reviews(self, barber_id: str) -> list[dict]:
        """Fetch all reviews for a barber to compute average rating."""
        url = f"{self._base_url}/api/reviews/barber/{barber_id}"
        try:
            async with httpx.AsyncClient(timeout=self._timeout) as client:
                response = await client.get(url, headers=self._headers)
                response.raise_for_status()
                return response.json()
        except httpx.TimeoutException:
            logger.warning("Timeout fetching reviews for barber %s", barber_id)
        except httpx.HTTPStatusError as exc:
            logger.warning("HTTP %s fetching reviews for barber %s", exc.response.status_code, barber_id)
        except Exception as exc:
            logger.error("Unexpected error fetching barber reviews: %s", exc)
        return []

    async def get_user_appointments(self) -> list[dict]:
        """Fetch the authenticated user's appointment history (requires JWT)."""
        url = f"{self._base_url}/api/appointments/my"
        try:
            async with httpx.AsyncClient(timeout=self._timeout) as client:
                response = await client.get(url, headers=self._headers)
                response.raise_for_status()
                return response.json()
        except httpx.TimeoutException:
            logger.warning("Timeout fetching user appointments")
        except httpx.HTTPStatusError as exc:
            logger.warning("HTTP %s fetching user appointments", exc.response.status_code)
        except Exception as exc:
            logger.error("Unexpected error fetching user appointments: %s", exc)
        return []
