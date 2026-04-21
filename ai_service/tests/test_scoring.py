"""
Unit tests for the deterministic scoring engine.
"""

from datetime import date, timedelta
from unittest.mock import AsyncMock, patch

import pytest

from models.schemas import SlotInfo
from services.scoring import (
    _barber_rating_score,
    _haircut_urgency_score,
    _preferred_time_score,
    rank_slots,
    score_slot,
)


# ---------------------------------------------------------------------------
# _barber_rating_score
# ---------------------------------------------------------------------------

class TestBarberRatingScore:
    def test_max_rating_gives_max_score(self):
        assert _barber_rating_score(5.0) == pytest.approx(25.0)

    def test_min_rating_gives_zero(self):
        assert _barber_rating_score(1.0) == pytest.approx(0.0)

    def test_mid_rating_is_half(self):
        assert _barber_rating_score(3.0) == pytest.approx(12.5)

    def test_missing_rating_returns_neutral(self):
        assert _barber_rating_score(None) == 15.0

    def test_above_max_is_clamped(self):
        assert _barber_rating_score(6.0) == pytest.approx(25.0)

    def test_below_min_is_clamped(self):
        assert _barber_rating_score(0.0) == pytest.approx(0.0)

    def test_score_within_range(self):
        for rating in [1.0, 2.0, 3.0, 4.0, 5.0]:
            score = _barber_rating_score(rating)
            assert 0.0 <= score <= 25.0


# ---------------------------------------------------------------------------
# _preferred_time_score
# ---------------------------------------------------------------------------

class TestPreferredTimeScore:
    def test_no_history_returns_neutral(self):
        assert _preferred_time_score("09:00:00", []) == 15.0

    def test_exact_match_returns_max(self):
        history = [{"startTime": "09:00:00"}, {"startTime": "09:30:00"}]
        score = _preferred_time_score("09:00:00", history)
        assert score == pytest.approx(30.0)

    def test_twelve_hour_diff_returns_zero(self):
        history = [{"startTime": "09:00:00"}]
        score = _preferred_time_score("21:00:00", history)
        assert score == pytest.approx(0.0)

    def test_three_hour_diff_is_partial(self):
        history = [{"startTime": "10:00:00"}]
        score = _preferred_time_score("13:00:00", history)
        assert 0.0 < score < 30.0

    def test_returns_neutral_for_unparseable_time(self):
        history = [{"startTime": "bad"}]
        score = _preferred_time_score("10:00:00", history)
        assert score == 15.0

    def test_uses_startTime_case_insensitive(self):
        history = [{"StartTime": "10:00:00"}]
        score = _preferred_time_score("10:00:00", history)
        assert score == pytest.approx(30.0)

    def test_score_within_range(self):
        history = [{"startTime": "12:00:00"}]
        for hour in range(0, 24):
            score = _preferred_time_score(f"{hour:02d}:00:00", history)
            assert 0.0 <= score <= 30.0


# ---------------------------------------------------------------------------
# _haircut_urgency_score
# ---------------------------------------------------------------------------

class TestHaircutUrgencyScore:
    def test_no_date_returns_neutral(self):
        assert _haircut_urgency_score(None) == 5.0

    def test_recent_7_days_returns_zero(self):
        recent = (date.today() - timedelta(days=7)).strftime("%Y-%m-%d")
        assert _haircut_urgency_score(recent) == 0.0

    def test_14_days_returns_three(self):
        d = (date.today() - timedelta(days=14)).strftime("%Y-%m-%d")
        assert _haircut_urgency_score(d) == 3.0

    def test_21_days_returns_six(self):
        d = (date.today() - timedelta(days=21)).strftime("%Y-%m-%d")
        assert _haircut_urgency_score(d) == 6.0

    def test_overdue_60_days_returns_max(self):
        overdue = (date.today() - timedelta(days=60)).strftime("%Y-%m-%d")
        assert _haircut_urgency_score(overdue) == 10.0

    def test_invalid_date_returns_neutral(self):
        assert _haircut_urgency_score("not-a-date") == 5.0

    def test_score_within_range(self):
        for days in [0, 7, 14, 21, 30, 45, 60]:
            d = (date.today() - timedelta(days=days)).strftime("%Y-%m-%d")
            assert 0.0 <= _haircut_urgency_score(d) <= 10.0


# ---------------------------------------------------------------------------
# score_slot (async)
# ---------------------------------------------------------------------------

@pytest.mark.asyncio
class TestScoreSlot:
    async def test_total_equals_sum_of_components(self):
        slot = SlotInfo(
            barberId="b1",
            barberName="Test",
            barberRating=5.0,
            serviceId="s1",
            date="2026-04-10",
            startTime="10:00:00",
            endTime="10:30:00",
        )
        result = await score_slot(
            slot=slot,
            user_id="u1",
            history=[],
            weather_score=20.0,
            last_haircut_date=None,
            include_calendar=False,
        )
        bd = result.score_breakdown
        expected = bd.barber_rating + bd.preferred_time_match + bd.weather_score + bd.calendar_availability + bd.haircut_urgency
        assert bd.total == pytest.approx(expected, abs=0.01)

    async def test_max_rating_slot_has_25_in_barber_component(self):
        slot = SlotInfo(
            barberId="b1", barberName="Pro", barberRating=5.0,
            serviceId="s1", date="2026-04-10", startTime="10:00:00", endTime="10:30:00",
        )
        result = await score_slot(slot, "u1", [], 15.0, None, False)
        assert result.score_breakdown.barber_rating == pytest.approx(25.0)

    async def test_calendar_conflict_sets_calendar_score_to_zero(self):
        slot = SlotInfo(
            barberId="b1", barberName="Pro", barberRating=4.0,
            serviceId="s1", date="2026-04-10", startTime="11:00:00", endTime="11:30:00",
        )
        with patch("services.scoring.check_slot_availability", return_value=(False, "conflict")):
            result = await score_slot(slot, "u1", [], 18.0, None, include_calendar=True)
        assert result.score_breakdown.calendar_availability == 0.0

    async def test_calendar_free_sets_calendar_score_to_15(self):
        slot = SlotInfo(
            barberId="b1", barberName="Pro", barberRating=4.0,
            serviceId="s1", date="2026-04-10", startTime="11:00:00", endTime="11:30:00",
        )
        with patch("services.scoring.check_slot_availability", return_value=(True, "available")):
            result = await score_slot(slot, "u1", [], 18.0, None, include_calendar=True)
        assert result.score_breakdown.calendar_availability == 15.0

    async def test_include_calendar_false_assumes_free(self):
        slot = SlotInfo(
            barberId="b1", barberName="Pro", barberRating=3.0,
            serviceId="s1", date="2026-04-10", startTime="09:00:00", endTime="09:30:00",
        )
        result = await score_slot(slot, "u1", [], 14.0, None, include_calendar=False)
        assert result.score_breakdown.calendar_availability == 15.0


# ---------------------------------------------------------------------------
# rank_slots (async, integration-style with mocked I/O)
# ---------------------------------------------------------------------------

@pytest.mark.asyncio
class TestRankSlots:
    async def test_returns_at_most_3_slots(self):
        slots = [
            SlotInfo(barberId="b1", barberName="B", barberRating=4.0, serviceId="s1",
                     date="2026-04-10", startTime=f"{h:02d}:00:00", endTime=f"{h:02d}:30:00")
            for h in range(8, 16)
        ]
        with (
            patch("services.scoring.get_weather_score", return_value=(15.0, "cloudy")),
            patch("services.scoring.DotNetClient") as MockClient,
        ):
            inst = MockClient.return_value
            inst.get_user_appointments = AsyncMock(return_value=[])
            inst.get_barber_reviews = AsyncMock(return_value=[])
            result = await rank_slots(slots, "u1", "Bucharest", None, False, None)
        assert len(result) <= 3

    async def test_sorted_descending_by_total(self):
        slots = [
            SlotInfo(barberId="low", barberName="Low", barberRating=1.0, serviceId="s1",
                     date="2026-04-10", startTime="09:00:00", endTime="09:30:00"),
            SlotInfo(barberId="high", barberName="High", barberRating=5.0, serviceId="s1",
                     date="2026-04-10", startTime="10:00:00", endTime="10:30:00"),
        ]
        with (
            patch("services.scoring.get_weather_score", return_value=(15.0, "cloudy")),
            patch("services.scoring.DotNetClient") as MockClient,
        ):
            inst = MockClient.return_value
            inst.get_user_appointments = AsyncMock(return_value=[])
            inst.get_barber_reviews = AsyncMock(return_value=[])
            result = await rank_slots(slots, "u1", None, None, False, None)
        assert result[0].score_breakdown.total >= result[-1].score_breakdown.total

    async def test_fetches_barber_rating_when_missing(self):
        slot = SlotInfo(
            barberId="b1", barberName="B", barberRating=None,
            serviceId="s1", date="2026-04-10", startTime="10:00:00", endTime="10:30:00",
        )
        reviews = [{"rating": 4}, {"rating": 5}]
        with (
            patch("services.scoring.get_weather_score", return_value=(15.0, "clear")),
            patch("services.scoring.DotNetClient") as MockClient,
        ):
            inst = MockClient.return_value
            inst.get_user_appointments = AsyncMock(return_value=[])
            inst.get_barber_reviews = AsyncMock(return_value=reviews)
            result = await rank_slots([slot], "u1", None, None, False, "token-xyz")
        # Rating should be (4+5)/2 = 4.5 → score = (3.5/4)*25 ≈ 21.875
        assert result[0].score_breakdown.barber_rating == pytest.approx(21.88, abs=0.1)
