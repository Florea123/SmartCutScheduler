"""
Hair analysis service using Google Gemini Vision.

Compares a reference photo (person right after a fresh haircut) with a
current photo to determine if the person needs a new haircut.
The analysis is fully vision-based — no measurements, only visual comparison.
"""

import asyncio
import json
import logging
from functools import partial
from typing import Optional

import httpx

from config import settings

logger = logging.getLogger(__name__)

_DEFAULT_IMAGE_MIME = "image/jpeg"

# ---------------------------------------------------------------------------
# Gemini Vision call (synchronous — run in executor)
# ---------------------------------------------------------------------------

_ANALYSIS_PROMPT = """\
You are an expert hair-length analysis AI assistant.
You will be shown exactly TWO photos:
  1. REFERENCE photo  — should show a real person right after a fresh haircut (the baseline).
  2. CURRENT photo    — should show the same real person today.

══════════════════════════════════════════════════════
STEP 1 — VALIDATION (check this FIRST, before any analysis)
══════════════════════════════════════════════════════
Before analyzing hair, you MUST validate the photos:

a) Does the REFERENCE photo clearly show a real human person (face or head visible)?
b) Does the CURRENT photo clearly show a real human person (face or head visible)?
c) Do both photos appear to show the SAME person (similar facial features, skin tone, general appearance)?

If validation fails, respond ONLY with this JSON and nothing else:
{
  "error": true,
  "error_type": "<see below>",
  "error_message": "<friendly Romanian message>"
}

Use one of these error_type values:
- "no_person_reference"  — reference photo does not contain a visible person
- "no_person_current"    — current photo does not contain a visible person
- "different_person"     — the two photos appear to show different people

Example error responses:
- { "error": true, "error_type": "no_person_reference", "error_message": "Poza de referință nu conține o persoană. Te rugăm să încarci o poză în care ești tu după o tunsoare proaspătă." }
- { "error": true, "error_type": "no_person_current", "error_message": "Poza actuală nu conține o persoană. Te rugăm să încarci o poză recentă cu tine." }
- { "error": true, "error_type": "different_person", "error_message": "Pozele par să conțină persoane diferite. Asigură-te că ambele poze sunt cu tine." }

══════════════════════════════════════════════════════
STEP 2 — HAIR ANALYSIS (only if validation passed)
══════════════════════════════════════════════════════
Compare the hair length in both photos and decide whether the person needs a haircut.

Guidelines:
- Focus on overall hair length, not style or colour differences.
- "needs_haircut" is true when hair growth is MODERATE or higher.
- "confidence" reflects how clearly visible and comparable the hair is
  (e.g. 0.9 if both photos show the full head clearly, lower if blurry/occluded).
- "hair_growth_level" must be exactly one of:
    none | minimal | moderate | significant | excessive | unknown
- "estimated_weeks_since_haircut" is your best integer estimate, or null if
  it cannot be reasonably inferred from the images.
- "reason" must be 2-3 concise, friendly sentences grounded only in what you see.

Reply ONLY with valid JSON — no markdown, no code fences, no extra text:
{
  "needs_haircut": true,
  "confidence": 0.85,
  "hair_growth_level": "moderate",
  "reason": "...",
  "estimated_weeks_since_haircut": 5
}
"""


def _call_gemini_vision(
    reference_bytes: bytes,
    current_bytes: bytes,
    reference_mime: str,
    current_mime: str,
) -> dict:
    """Synchronous Gemini multimodal call — executed in a thread-pool executor."""
    import base64
    from google import genai  # lazy import to keep startup fast
    from google.genai import types

    client = genai.Client(api_key=settings.gemini_api_key)

    parts = [
        types.Part(text="REFERENCE photo (fresh haircut — the baseline):"),
        types.Part(
            inline_data=types.Blob(
                mime_type=reference_mime,
                data=base64.b64encode(reference_bytes).decode("utf-8"),
            )
        ),
        types.Part(text="CURRENT photo (how the person looks today):"),
        types.Part(
            inline_data=types.Blob(
                mime_type=current_mime,
                data=base64.b64encode(current_bytes).decode("utf-8"),
            )
        ),
        types.Part(text=_ANALYSIS_PROMPT),
    ]

    response = client.models.generate_content(
        model=settings.gemini_vision_model,
        contents=parts,
        config=types.GenerateContentConfig(
            response_mime_type="application/json",
            temperature=0.1,
            max_output_tokens=400,
        ),
    )

    return json.loads(response.text)


# ---------------------------------------------------------------------------
# Public async interface
# ---------------------------------------------------------------------------

async def analyze_hair(
    reference_bytes: bytes,
    current_bytes: bytes,
    reference_mime: str = _DEFAULT_IMAGE_MIME,
    current_mime: str = _DEFAULT_IMAGE_MIME,
) -> dict:
    """
    Compare a fresh-haircut reference photo with a current photo.

    Returns a dict with keys:
        needs_haircut, confidence, hair_growth_level,
        reason, estimated_weeks_since_haircut
    Falls back gracefully when Gemini is unavailable.
    """
    if not settings.gemini_api_key:
        logger.info("Gemini API key not set — skipping hair analysis")
        return _fallback_response("Gemini API key not configured — analysis skipped.")

    loop = asyncio.get_event_loop()
    try:
        result = await loop.run_in_executor(
            None,
            partial(
                _call_gemini_vision,
                reference_bytes,
                current_bytes,
                reference_mime,
                current_mime,
            ),
        )

        # If Gemini returned a validation error, propagate it as-is
        if result.get("error"):
            logger.warning("Hair analysis validation failed: %s", result.get("error_type"))
            return result

        logger.info(
            "Hair analysis: needs_haircut=%s  growth=%s  confidence=%.2f",
            result.get("needs_haircut"),
            result.get("hair_growth_level"),
            result.get("confidence", 0.0),
        )
        return result
    except json.JSONDecodeError as exc:
        logger.error("Gemini returned invalid JSON for hair analysis: %s", exc)
        return _fallback_response("AI returned an unexpected response — please try again.")
    except Exception as exc:
        logger.error("Hair analysis failed: %s", exc)
        return _fallback_response(f"Analysis unavailable: {exc}")


async def fetch_image_from_url(url: str) -> tuple[bytes, str]:
    """
    Download an image from a URL.

    Returns (raw_bytes, mime_type).
    Raises httpx.HTTPError on failure.
    """
    # If the URL is relative (e.g. /profile-images/...) prepend the backend base
    if url.startswith("/"):
        url = settings.dotnet_api_url.rstrip("/") + url

    async with httpx.AsyncClient(timeout=15) as client:
        response = await client.get(url)
        response.raise_for_status()
        content_type = response.headers.get("content-type", _DEFAULT_IMAGE_MIME).split(";")[0].strip()
        return response.content, content_type


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _fallback_response(reason: str) -> dict:
    return {
        "needs_haircut": False,
        "confidence": 0.0,
        "hair_growth_level": "unknown",
        "reason": reason,
        "estimated_weeks_since_haircut": None,
    }


def build_recommendation_message(growth_level: str, estimated_weeks: Optional[int]) -> str:
    """Return a friendly Romanian-language recommendation message."""
    weeks_hint = (
        f" (estimat: ~{estimated_weeks} săptămâni de la ultima tunsoare)"
        if estimated_weeks
        else ""
    )
    messages = {
        "moderate": f"Părul tău a crescut suficient{weeks_hint}. E un moment bun să te programezi!",
        "significant": f"Părul tău a crescut destul de mult{weeks_hint}. Recomandăm să te tunzi în curând.",
        "excessive": f"Părul tău a crescut foarte mult{weeks_hint}. E timpul pentru o tunsoare!",
    }
    return messages.get(
        growth_level,
        f"Ai putea lua în considerare o tunsoare{weeks_hint}.",
    )
