import pytest
from httpx import ASGITransport, AsyncClient

from main import app
from models.schemas import ScoreBreakdown, ScoredSlot, SlotInfo


# ---------------------------------------------------------------------------
# Reusable slot fixtures
# ---------------------------------------------------------------------------

@pytest.fixture
def slot_morning() -> SlotInfo:
    return SlotInfo(
        barberId="barber-1",
        barberName="Ion Popescu",
        barberRating=4.5,
        serviceId="svc-1",
        date="2026-04-10",
        startTime="09:00:00",
        endTime="09:30:00",
    )


@pytest.fixture
def slot_afternoon() -> SlotInfo:
    return SlotInfo(
        barberId="barber-2",
        barberName="Andrei Ionescu",
        barberRating=3.0,
        serviceId="svc-2",
        date="2026-04-10",
        startTime="14:00:00",
        endTime="14:30:00",
    )


@pytest.fixture
def slot_evening() -> SlotInfo:
    return SlotInfo(
        barberId="barber-1",
        barberName="Ion Popescu",
        barberRating=4.8,
        serviceId="svc-1",
        date="2026-04-10",
        startTime="18:00:00",
        endTime="18:30:00",
    )


@pytest.fixture
def sample_slots(slot_morning, slot_afternoon, slot_evening) -> list[SlotInfo]:
    return [slot_morning, slot_afternoon, slot_evening]


def make_scored_slot(barber_name: str, hour: int, total: float) -> ScoredSlot:
    """Helper to build a ScoredSlot with a specific total score."""
    remainder = max(0.0, total - 75.0)
    return ScoredSlot(
        slot=SlotInfo(
            barberId="b-test",
            barberName=barber_name,
            barberRating=4.0,
            serviceId="s-test",
            date="2026-04-10",
            startTime=f"{hour:02d}:00:00",
            endTime=f"{hour:02d}:30:00",
        ),
        score_breakdown=ScoreBreakdown(
            barber_rating=20.0,
            preferred_time_match=25.0,
            weather_score=15.0,
            calendar_availability=15.0,
            haircut_urgency=remainder,
            total=total,
        ),
    )


# ---------------------------------------------------------------------------
# Async HTTP client for endpoint tests
# ---------------------------------------------------------------------------

@pytest.fixture
async def async_client():
    async with AsyncClient(
        transport=ASGITransport(app=app), base_url="http://test"
    ) as client:
        yield client
