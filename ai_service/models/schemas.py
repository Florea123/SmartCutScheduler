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


# ---------------------------------------------------------------------------
# Haircut AI schemas
# ---------------------------------------------------------------------------

class HaircutAnalysisResponse(BaseModel):
    # Validation error fields (set when photo validation fails)
    error: bool = False
    error_type: Optional[str] = None   # no_person_reference | no_person_current | different_person
    error_message: Optional[str] = None

    # Analysis fields (set when validation passes)
    needs_haircut: bool = False
    confidence: float = Field(default=0.0, ge=0.0, le=1.0)
    hair_growth_level: str = "unknown"  # none | minimal | moderate | significant | excessive | unknown
    reason: str = ""
    estimated_weeks_since_haircut: Optional[int] = None
    recommendation_message: Optional[str] = None  # populated when needs_haircut=True


class FreshPhotoSavedResponse(BaseModel):
    message: str
    photo_url: str
