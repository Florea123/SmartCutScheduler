"""
Unit tests for services/gemini_service.py
"""

import json
from unittest.mock import AsyncMock, MagicMock, patch

import pytest

from models.schemas import ScoreBreakdown, ScoredSlot, SlotInfo
from services.gemini_service import _build_prompt, refine_with_gemini


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _make_slot(barber: str, hour: int, total: float) -> ScoredSlot:
    return ScoredSlot(
        slot=SlotInfo(
            barberId="b1",
            barberName=barber,
            barberRating=4.0,
            serviceId="s1",
            date="2026-04-28",
            startTime=f"{hour:02d}:00:00",
            endTime=f"{hour:02d}:30:00",
        ),
        score_breakdown=ScoreBreakdown(
            barber_rating=20.0,
            preferred_time_match=25.0,
            weather_score=15.0,
            calendar_availability=15.0,
            haircut_urgency=total - 75.0 if total > 75 else 0.0,
            total=total,
        ),
    )


# ---------------------------------------------------------------------------
# _build_prompt
# ---------------------------------------------------------------------------

class TestBuildPrompt:
    def test_prompt_includes_barber_names(self):
        slots = [_make_slot("Ion", 9, 85.0), _make_slot("Andrei", 14, 72.0)]
        prompt = _build_prompt(slots, "Bucharest")
        assert "Ion" in prompt
        assert "Andrei" in prompt

    def test_prompt_includes_city(self):
        slots = [_make_slot("Ion", 9, 85.0)]
        prompt = _build_prompt(slots, "Iasi")
        assert "Iasi" in prompt

    def test_prompt_without_city_no_location_line(self):
        slots = [_make_slot("Ion", 9, 85.0)]
        prompt = _build_prompt(slots, None)
        assert "located in" not in prompt

    def test_prompt_includes_score_components(self):
        slots = [_make_slot("Ion", 9, 85.0)]
        prompt = _build_prompt(slots, None)
        assert "Barber rating" in prompt
        assert "Time preference" in prompt
        assert "Weather" in prompt

    def test_prompt_numbered_options(self):
        slots = [_make_slot("A", 9, 90.0), _make_slot("B", 10, 80.0), _make_slot("C", 11, 70.0)]
        prompt = _build_prompt(slots, None)
        assert "Option 1" in prompt
        assert "Option 2" in prompt
        assert "Option 3" in prompt

    def test_prompt_requests_json_response(self):
        slots = [_make_slot("Ion", 9, 85.0)]
        prompt = _build_prompt(slots, None)
        assert "chosen_option" in prompt
        assert "reason" in prompt


# ---------------------------------------------------------------------------
# refine_with_gemini — no API key
# ---------------------------------------------------------------------------

@pytest.mark.asyncio
class TestRefineWithGeminiNoApiKey:
    async def test_returns_index_0_when_no_api_key(self):
        slots = [_make_slot("Ion", 9, 85.0)]
        with patch("services.gemini_service.settings") as mock_settings:
            mock_settings.gemini_api_key = ""
            idx, reason = await refine_with_gemini(slots)
        assert idx == 0
        assert isinstance(reason, str)

    async def test_reason_is_non_empty_when_no_api_key(self):
        slots = [_make_slot("Ion", 9, 85.0)]
        with patch("services.gemini_service.settings") as mock_settings:
            mock_settings.gemini_api_key = ""
            _, reason = await refine_with_gemini(slots)
        assert len(reason) > 0


# ---------------------------------------------------------------------------
# refine_with_gemini — Gemini call paths
# ---------------------------------------------------------------------------

@pytest.mark.asyncio
class TestRefineWithGeminiApiPaths:
    async def test_successful_call_returns_chosen_index(self):
        raw_response = json.dumps({"chosen_option": 2, "reason": "Second is best."})
        slots = [_make_slot("A", 9, 90.0), _make_slot("B", 14, 80.0)]

        with (
            patch("services.gemini_service.settings") as mock_settings,
            patch("services.gemini_service.asyncio.get_event_loop") as mock_loop,
        ):
            mock_settings.gemini_api_key = "fake-key"
            loop = MagicMock()
            loop.run_in_executor = AsyncMock(return_value=raw_response)
            mock_loop.return_value = loop

            idx, reason = await refine_with_gemini(slots, "Bucharest")

        assert idx == 1  # 0-based
        assert "Second is best." in reason

    async def test_chosen_option_1_returns_index_0(self):
        raw_response = json.dumps({"chosen_option": 1, "reason": "First is best."})
        slots = [_make_slot("A", 9, 90.0), _make_slot("B", 14, 80.0)]

        with (
            patch("services.gemini_service.settings") as mock_settings,
            patch("services.gemini_service.asyncio.get_event_loop") as mock_loop,
        ):
            mock_settings.gemini_api_key = "fake-key"
            loop = MagicMock()
            loop.run_in_executor = AsyncMock(return_value=raw_response)
            mock_loop.return_value = loop

            idx, _ = await refine_with_gemini(slots)

        assert idx == 0

    async def test_out_of_range_chosen_clamped(self):
        # Gemini returns option 10 but only 2 slots → clamped to 1 (last valid)
        raw_response = json.dumps({"chosen_option": 10, "reason": "Way out of range."})
        slots = [_make_slot("A", 9, 90.0), _make_slot("B", 14, 80.0)]

        with (
            patch("services.gemini_service.settings") as mock_settings,
            patch("services.gemini_service.asyncio.get_event_loop") as mock_loop,
        ):
            mock_settings.gemini_api_key = "fake-key"
            loop = MagicMock()
            loop.run_in_executor = AsyncMock(return_value=raw_response)
            mock_loop.return_value = loop

            idx, _ = await refine_with_gemini(slots)

        assert 0 <= idx <= len(slots) - 1

    async def test_markdown_fences_stripped_from_response(self):
        raw_fenced = "```json\n" + json.dumps({"chosen_option": 1, "reason": "Fenced."}) + "\n```"
        slots = [_make_slot("A", 9, 90.0)]

        with (
            patch("services.gemini_service.settings") as mock_settings,
            patch("services.gemini_service.asyncio.get_event_loop") as mock_loop,
        ):
            mock_settings.gemini_api_key = "fake-key"
            loop = MagicMock()
            loop.run_in_executor = AsyncMock(return_value=raw_fenced)
            mock_loop.return_value = loop

            idx, reason = await refine_with_gemini(slots)

        assert idx == 0
        assert "Fenced." in reason

    async def test_json_parse_error_falls_back_to_index_0(self):
        slots = [_make_slot("A", 9, 90.0), _make_slot("B", 14, 80.0)]

        with (
            patch("services.gemini_service.settings") as mock_settings,
            patch("services.gemini_service.asyncio.get_event_loop") as mock_loop,
        ):
            mock_settings.gemini_api_key = "fake-key"
            loop = MagicMock()
            loop.run_in_executor = AsyncMock(return_value="not valid json!!!")
            mock_loop.return_value = loop

            idx, reason = await refine_with_gemini(slots)

        assert idx == 0
        assert isinstance(reason, str)

    async def test_api_exception_falls_back_to_index_0(self):
        slots = [_make_slot("A", 9, 90.0)]

        with (
            patch("services.gemini_service.settings") as mock_settings,
            patch("services.gemini_service.asyncio.get_event_loop") as mock_loop,
        ):
            mock_settings.gemini_api_key = "fake-key"
            loop = MagicMock()
            loop.run_in_executor = AsyncMock(side_effect=RuntimeError("API down"))
            mock_loop.return_value = loop

            idx, reason = await refine_with_gemini(slots)

        assert idx == 0
        assert isinstance(reason, str)

    async def test_city_included_in_prompt(self):
        """Verify city is forwarded into the prompt (no exception)."""
        raw_response = json.dumps({"chosen_option": 1, "reason": "OK."})
        slots = [_make_slot("A", 9, 90.0)]

        with (
            patch("services.gemini_service.settings") as mock_settings,
            patch("services.gemini_service.asyncio.get_event_loop") as mock_loop,
        ):
            mock_settings.gemini_api_key = "fake-key"
            loop = MagicMock()
            loop.run_in_executor = AsyncMock(return_value=raw_response)
            mock_loop.return_value = loop

            idx, reason = await refine_with_gemini(slots, user_city="Cluj")

        assert idx == 0
        assert "OK." in reason
