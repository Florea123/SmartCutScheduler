"""
Deterministic scoring engine.

Final Score (0–100) = sum of five components:
  barber_rating          0–25   (based on average review rating)
  preferred_time_match   0–30   (how well the slot matches user's history)
  weather_score          0–20   (current weather at user's city)
  calendar_availability  0–15   (no Google Calendar conflicts)
  haircut_urgency        0–10   (days since last haircut)
"""

import asyncio
import logging
from datetime import date, datetime
from typing import Optional

from config import settings
from models.schemas import ScoreBreakdown, ScoredSlot, SlotInfo
from services.calendar_service import check_slot_availability, is_calendar_connected
from services.dotnet_client import DotNetClient
from services.weather_service import get_weather_score

logger = logging.getLogger(__name__)


# ---------------------------------------------------------------------------
# Individual component scorers
# ---------------------------------------------------------------------------

def _barber_rating_score(avg_rating: Optional[float]) -> float:
    """Convert 1–5 star average rating to 0–25. Missing → neutral 15."""
    if avg_rating is None:
        return 15.0
    clamped = max(1.0, min(5.0, avg_rating))
    return round(((clamped - 1.0) / 4.0) * settings.weight_barber_rating, 2)


def _preferred_time_score(start_time: str, history: list[dict]) -> float:
    """
    Compare slot hour with the user's historically preferred hour.
    Returns 0–30. Neutral (15) when no history is available.
    Diff of 0 h → 30 pts, diff of 12 h → 0 pts (linear).
    """
    if not history:
        return 15.0

    hours: list[int] = []
    for appt in history:
        raw = appt.get("startTime") or appt.get("StartTime", "")
        if raw and len(raw) >= 2:
            try:
                hours.append(int(str(raw)[:2]))
            except ValueError:
                pass

    if not hours:
        return 15.0

    avg_hour = sum(hours) / len(hours)
    try:
        slot_hour = int(str(start_time)[:2])
    except (ValueError, IndexError):
        return 15.0

    diff = abs(slot_hour - avg_hour)
    return round(max(0.0, (1.0 - diff / 12.0)) * settings.weight_preferred_time, 2)


def _haircut_urgency_score(last_haircut_date: Optional[str]) -> float:
    """
    Score 0–10 based on days since last haircut.
    More days overdue → higher score (user needs it more).
    """
    if not last_haircut_date:
        return 5.0  # neutral

    try:
        last = datetime.strptime(last_haircut_date, "%Y-%m-%d").date()
        days = (date.today() - last).days
    except ValueError:
        return 5.0

    if days < 14:
        return 0.0
    if days < 21:
        return 3.0
    if days < 30:
        return 6.0
    if days < 45:
        return 8.0
    return settings.weight_urgency  # 10.0


# ---------------------------------------------------------------------------
# Slot scorer
# ---------------------------------------------------------------------------

async def score_slot(
    slot: SlotInfo,
    user_id: str,
    history: list[dict],
    weather_score: float,
    last_haircut_date: Optional[str],
    include_calendar: bool,
) -> ScoredSlot:
    br = _barber_rating_score(slot.barberRating)
    pt = _preferred_time_score(slot.startTime, history)
    w = weather_score

    if include_calendar:
        available, reason = await check_slot_availability(
            user_id, slot.date, slot.startTime, slot.endTime
        )
        cal = settings.weight_calendar if available else 0.0
        logger.debug("Calendar %s@%s → %s (%s)", slot.date, slot.startTime, available, reason)
    else:
        cal = settings.weight_calendar  # assume free

    urgency = _haircut_urgency_score(last_haircut_date)
    total = br + pt + w + cal + urgency

    return ScoredSlot(
        slot=slot,
        score_breakdown=ScoreBreakdown(
            barber_rating=round(br, 2),
            preferred_time_match=round(pt, 2),
            weather_score=round(w, 2),
            calendar_availability=round(cal, 2),
            haircut_urgency=round(urgency, 2),
            total=round(total, 2),
        ),
    )


# ---------------------------------------------------------------------------
# Main ranking function
# ---------------------------------------------------------------------------

async def rank_slots(
    slots: list[SlotInfo],
    user_id: str,
    city: Optional[str],
    last_haircut_date: Optional[str],
    include_calendar: bool,
    auth_token: Optional[str],
) -> list[ScoredSlot]:
    """
    Score every slot, return the top 3 sorted by total score descending.
    Fetches weather once and user history once; barber ratings fetched on demand.
    """
    client = DotNetClient(auth_token=auth_token)

    # 1. Weather (one call for all slots)
    use_city = city or settings.default_city
    weather_val, weather_label = await get_weather_score(use_city)
    logger.info("Weather for '%s': %.1f (%s)", use_city, weather_val, weather_label)

    # 2. User appointment history (requires JWT)
    history: list[dict] = await client.get_user_appointments() if auth_token else []

    # 3. Resolve actual calendar flag: only True if user explicitly opted in AND has connected
    effective_calendar = include_calendar and is_calendar_connected(user_id)
    if include_calendar and not effective_calendar:
        logger.info("User %s requested calendar check but no token found — skipping", user_id)

    # 4. Enrich slots with barber ratings fetched from .NET API (cached per barber)
    rating_cache: dict[str, Optional[float]] = {}
    enriched: list[SlotInfo] = []
    for slot in slots:
        if slot.barberRating is None:
            if slot.barberId not in rating_cache:
                reviews = await client.get_barber_reviews(slot.barberId)
                ratings = [
                    float(r["rating"])
                    for r in reviews
                    if r.get("rating") is not None
                ]
                rating_cache[slot.barberId] = sum(ratings) / len(ratings) if ratings else None
            slot = slot.model_copy(update={"barberRating": rating_cache[slot.barberId]})
        enriched.append(slot)

    # 5. Score all slots concurrently
    scored: tuple[ScoredSlot, ...] = await asyncio.gather(
        *[
            score_slot(
                slot=s,
                user_id=user_id,
                history=history,
                weather_score=weather_val,
                last_haircut_date=last_haircut_date,
                include_calendar=effective_calendar,
            )
            for s in enriched
        ]
    )

    return sorted(scored, key=lambda x: x.score_breakdown.total, reverse=True)[:3]
