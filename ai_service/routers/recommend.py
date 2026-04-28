import logging

from fastapi import APIRouter, HTTPException

from models.schemas import RecommendRequest, RecommendResponse, RecommendedSlot
from services.gemini_service import refine_with_gemini
from services.scoring import rank_slots

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/recommend", tags=["recommend"])


@router.post(
    "",
    summary="Get AI-powered appointment recommendation",
    responses={
        400: {"description": "No available slots provided"},
        422: {"description": "Could not score any of the provided slots"},
    },
)
async def recommend(request: RecommendRequest) -> RecommendResponse:
    """
    Score all provided slots using the deterministic engine, then ask
    Google Gemini to select and explain the best one.

    - **city** — pass the user's city for accurate weather scoring
    - **includeCalendar** — set `true` if the user has connected their Google Calendar
    - **authToken** — optional JWT forwarded to the .NET API for user history
    - **lastHaircutDate** — ISO date used for urgency scoring
    """
    if not request.availableSlots:
        raise HTTPException(status_code=400, detail="No available slots provided.")

    top_3 = await rank_slots(
        slots=request.availableSlots,
        user_id=request.userId,
        city=request.city,
        last_haircut_date=request.lastHaircutDate,
        include_calendar=request.includeCalendar,
        auth_token=request.authToken,
    )

    if not top_3:
        raise HTTPException(status_code=422, detail="Could not score any of the provided slots.")

    chosen_index, reason = await refine_with_gemini(top_3, user_city=request.city)
    best = top_3[chosen_index]

    logger.info(
        "Recommendation for user %s: %s @ %s (score=%.1f)",
        request.userId,
        best.slot.barberName,
        best.slot.startTime,
        best.score_breakdown.total,
    )

    return RecommendResponse(
        recommended_slot=RecommendedSlot(
            date=best.slot.date,
            time=best.slot.startTime,
            barber=best.slot.barberName,
            barberId=best.slot.barberId,
            serviceId=best.slot.serviceId,
            endTime=best.slot.endTime,
        ),
        reason=reason,
        score_breakdown=best.score_breakdown,
        top_3=top_3,
    )
