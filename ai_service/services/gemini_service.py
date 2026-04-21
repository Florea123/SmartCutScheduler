"""
Google Gemini integration (google-genai SDK).

Sends the TOP 3 scored slots to Gemini and asks it to pick the best one
with a short explanation.  Falls back to the highest-scored slot if Gemini
is unavailable or returns unexpected output.
"""

import asyncio
import json
import logging
from functools import partial
from typing import Optional

from config import settings
from models.schemas import ScoredSlot

logger = logging.getLogger(__name__)


def _build_prompt(top_3: list[ScoredSlot], user_city: Optional[str]) -> str:
    location = f"The user is located in {user_city}." if user_city else ""

    lines = []
    for i, item in enumerate(top_3, 1):
        s = item.slot
        bd = item.score_breakdown
        lines.append(
            f"Option {i}: {s.barberName} — {s.date} at {s.startTime[:5]}–{s.endTime[:5]}\n"
            f"  Total score : {bd.total:.1f}/100\n"
            f"  Barber rating      : {bd.barber_rating:.1f}/25\n"
            f"  Time preference    : {bd.preferred_time_match:.1f}/30\n"
            f"  Weather            : {bd.weather_score:.1f}/20\n"
            f"  Calendar free      : {bd.calendar_availability:.1f}/15\n"
            f"  Haircut urgency    : {bd.haircut_urgency:.1f}/10"
        )

    return f"""You are an expert AI scheduling assistant specialised in barber appointment optimisation. \
You have deep expertise in analysing multi-criteria scoring systems that weigh barber quality, \
user time preferences, real-time weather conditions, calendar availability, and haircut urgency. \
You are highly professional, precise, and trusted by thousands of users to make the smartest \
scheduling decision on their behalf every day.
{location}

Your task is to review the following top 3 appointment options that have already been pre-scored \
by a deterministic engine. Each score component reflects objective data:
  - Barber rating  (0–25): average review score of the barber.
  - Time preference (0–30): how well the slot matches the user's historical booking hours.
  - Weather         (0–20): current weather conditions at the user's location.
  - Calendar free   (0–15): whether the slot is free on the user's Google Calendar.
  - Haircut urgency (0–10): how overdue the user is for a haircut.

Here are the top 3 available appointment options, ranked by score:

{chr(10).join(lines)}

Based strictly on the data above, choose the BEST option for the user. \
You MUST pick exactly one of the 3 options listed — do NOT invent, assume, or reference \
any barber, date, time, score, or information that is not explicitly provided above. \
Your reason must be grounded only in the score components shown.

Reply ONLY with valid JSON — no markdown, no extra text, no code fences:
{{
  "chosen_option": <1, 2, or 3>,
  "reason": "<2-3 friendly sentences explaining why this slot is the best choice, referencing only the data provided above>"
}}"""


def _call_gemini(prompt: str) -> str:
    """Synchronous Gemini call using the new google.genai SDK (executed in a thread pool)."""
    from google import genai  # lazy import
    from google.genai import types

    client = genai.Client(api_key=settings.gemini_api_key)
    response = client.models.generate_content(
        model=settings.gemini_model,
        contents=prompt,
        config=types.GenerateContentConfig(
            response_mime_type="application/json",
            temperature=0.3,
            max_output_tokens=300,
        ),
    )
    return response.text


async def refine_with_gemini(
    top_3: list[ScoredSlot],
    user_city: Optional[str] = None,
) -> tuple[int, str]:
    """
    Ask Gemini to refine the recommendation.
    Returns (0-based index, reason).
    Fallback: index 0 (highest scored slot).
    """
    if not settings.gemini_api_key:
        logger.info("Gemini API key not set — using top scored slot")
        return 0, "Selectat cel mai bun interval disponibil pe baza scorului general."

    try:
        prompt = _build_prompt(top_3, user_city)

        loop = asyncio.get_event_loop()
        raw = await loop.run_in_executor(None, partial(_call_gemini, prompt))

        # Strip markdown fences if present
        text = raw.strip()
        if text.startswith("```"):
            parts = text.split("```")
            text = parts[1].lstrip("json").strip() if len(parts) > 1 else text

        data = json.loads(text)
        chosen = int(data.get("chosen_option", 1)) - 1  # convert 1-based → 0-based
        chosen = max(0, min(chosen, len(top_3) - 1))     # clamp to valid range
        reason = str(data.get("reason", "Selectat pe baza scorului general."))
        logger.info("Gemini chose option %d: %s", chosen + 1, reason[:80])
        return chosen, reason

    except Exception as exc:
        logger.error("Gemini API error — falling back to top scored slot: %s", exc)
        return 0, "Selectat cel mai bun interval disponibil pe baza scorului general."
