from pydantic import BaseModel, Field
from typing import Optional


class SlotInfo(BaseModel):
    barberId: str
    barberName: str
    barberRating: Optional[float] = None  # 1.0–5.0; fetched from .NET API if absent
    serviceId: str
    date: str       # "YYYY-MM-DD"
    startTime: str  # "HH:MM:SS"
    endTime: str    # "HH:MM:SS"


class RecommendRequest(BaseModel):
    userId: str
    availableSlots: list[SlotInfo] = Field(..., min_length=1)
    city: Optional[str] = None           # e.g. "Bucharest" — for weather scoring
    includeCalendar: bool = False        # check Google Calendar for conflicts
    authToken: Optional[str] = None     # user JWT forwarded to .NET API
    lastHaircutDate: Optional[str] = None  # "YYYY-MM-DD" — for urgency scoring


class ScoreBreakdown(BaseModel):
    barber_rating: float
    preferred_time_match: float
    weather_score: float
    calendar_availability: float
    haircut_urgency: float
    total: float


class ScoredSlot(BaseModel):
    slot: SlotInfo
    score_breakdown: ScoreBreakdown


class RecommendedSlot(BaseModel):
    date: str
    time: str
    barber: str
    barberId: str
    serviceId: str
    endTime: str


class RecommendResponse(BaseModel):
    recommended_slot: RecommendedSlot
    reason: str
    score_breakdown: ScoreBreakdown
    top_3: list[ScoredSlot]


class CalendarConnectResponse(BaseModel):
    auth_url: str


class CalendarStatusResponse(BaseModel):
    connected: bool
