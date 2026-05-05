"""
Hair analysis service using Google Gemini Vision.

Compares a reference photo (person right after a fresh haircut) with a
current photo to determine if the person needs a new haircut.
The analysis is fully vision-based — no measurements, only visual comparison.
"""

import asyncio
import json
import logging
import urllib.parse
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
You are a precise hair-growth comparison assistant. Your ONLY job is to measure
how much the hair has grown RELATIVE to the reference photo. You are NOT judging
absolute hair length — you are measuring the DIFFERENCE between two photos of
the same person.

You will be shown exactly TWO photos:
  1. REFERENCE photo — taken RIGHT AFTER a fresh haircut. This is the ZERO point.
     Whatever hair length you see here means growth = 0.
  2. CURRENT photo   — taken today. Compare against REFERENCE to detect growth.

══════════════════════════════════════════════════════
STEP 1 — VALIDATION (check this FIRST, before any analysis)
══════════════════════════════════════════════════════
Check the following before doing any analysis:

a) Does the REFERENCE photo clearly show a real human person with visible head/hair?
b) Does the CURRENT photo clearly show a real human person with visible head/hair?
c) Do both photos appear to be the SAME person (similar face, skin tone, features)?

If any check fails, respond ONLY with this JSON and nothing else:
{
  "error": true,
  "error_type": "<see below>",
  "error_message": "<friendly Romanian message>"
}

error_type values:
- "no_person_reference"  — reference photo does not show a visible person
- "no_person_current"    — current photo does not show a visible person
- "different_person"     — the photos appear to show different people

Example error responses:
- { "error": true, "error_type": "no_person_reference", "error_message": "Poza de referință nu conține o persoană. Te rugăm să încarci o poză în care ești tu după o tunsoare proaspătă." }
- { "error": true, "error_type": "no_person_current", "error_message": "Poza actuală nu conține o persoană. Te rugăm să încarci o poză recentă cu tine." }
- { "error": true, "error_type": "different_person", "error_message": "Pozele par să conțină persoane diferite. Asigură-te că ambele poze sunt cu tine." }

══════════════════════════════════════════════════════
STEP 2 — ANGLE ASSESSMENT (do this before comparing)
══════════════════════════════════════════════════════
Determine the angle/pose in each photo:
  - Is the head facing front, 3/4, profile (side), or top-down?

Then apply these rules:

  ANGLE ILLUSIONS — things that look like growth but are NOT:
  ┌─────────────────────────────────────────────────────────────────────────┐
  │ 1. SIDE FADE / SIDES: A skin fade or tight side always looks LOOSER    │
  │    from a profile or 3/4 angle than from the front. Do NOT interpret   │
  │    this as hair growing out. Fades are only comparable when both       │
  │    photos show the sides from the same angle.                          │
  │                                                                        │
  │ 2. TOP VOLUME: Hair on top appears flatter when photographed from      │
  │    the front and fuller from a lower or side angle. Volume alone is    │
  │    not evidence of growth if the photos are from different angles.     │
  │                                                                        │
  │ 3. NECKLINE: Visible from the back/side only. Not comparable if one   │
  │    photo shows the back and the other shows the front.                 │
  └─────────────────────────────────────────────────────────────────────────┘

  RELIABLE SIGNALS (valid even across angles):
  ┌─────────────────────────────────────────────────────────────────────────┐
  │ • HAIR OVERHANGING THE EARS: Visible from front AND side. If the hair  │
  │   was above the ear in the reference and now covers it, that IS growth.│
  │                                                                        │
  │ • HAIR LENGTH ON TOP (absolute): If in the reference the top hair is  │
  │   under 2 cm and in the current photo strands are clearly 4+ cm and   │
  │   falling, that is growth — provided you can see the same region.     │
  │                                                                        │
  │ • OVERALL SILHOUETTE SHAPE: If the head shape is noticeably more      │
  │   rounded or the hair extends much further from the skull in ALL       │
  │   visible directions, that is growth. One direction alone may be       │
  │   an angle effect.                                                     │
  └─────────────────────────────────────────────────────────────────────────┘

══════════════════════════════════════════════════════
STEP 3 — GROWTH DELTA ANALYSIS
══════════════════════════════════════════════════════

STEP 3A — Describe the REFERENCE photo:
  - Head angle/pose
  - Hair length on top (approximate: buzzed <5mm / very short 5-15mm / short 15-30mm / medium 30-60mm / long 60mm+)
  - Sides: skin fade / close fade / trimmed / full
  - Neckline: sharp/clean or growing out?

STEP 3B — Describe the CURRENT photo:
  - Head angle/pose
  - Hair length on top
  - Sides
  - Neckline

STEP 3C — Compare using ONLY angle-reliable signals from Step 2:
  For each region, explicitly state:
    - "This comparison is RELIABLE (same angle or angle-independent signal)"
    - "This comparison is UNRELIABLE (angle difference makes this misleading)"
  Only count RELIABLE observations as evidence of growth.

STEP 3D — Assign growth level based ONLY on reliable evidence:
  "none"        → No reliable visual difference.
  "minimal"     → Barely perceptible growth from ONE reliable signal only.
  "moderate"    → Clearly visible growth from at least ONE reliable signal.
                  Must be something that cannot be explained by angle difference.
  "significant" → Noticeably longer overall from multiple reliable signals.
  "excessive"   → Dramatically longer, obvious even across different angles.
  "unknown"     → Cannot make ANY reliable comparison (both photos too blurry/incomplete).

STEP 3E — Final values:
  - "needs_haircut": true when growth_level is "moderate", "significant", or "excessive".
  - "confidence": (0.0–1.0). Reduce by 0.1–0.2 for each comparison region that is
      UNRELIABLE due to angle. If ALL compared regions are unreliable, confidence < 0.4.
  - "estimated_weeks_since_haircut": a SINGLE INTEGER or null.
      none/minimal  → null
      moderate      → integer between 3 and 5 (e.g. 4)
      significant   → integer between 6 and 10 (e.g. 7)
      excessive     → integer above 10 (e.g. 12)
  - "reason": 2–3 sentences. MUST explicitly state which signals were reliable and
      which were discarded due to angle. Example: "The fade on the sides appears less
      sharp in the current photo, but this is likely due to the profile angle and was
      discounted. The hair on top, which is visible in both photos, appears to be at
      a similar length, so no significant growth was detected."

Reply ONLY with valid JSON — no markdown, no code fences, no extra text:
{
  "needs_haircut": false,
  "confidence": 0.75,
  "hair_growth_level": "none",
  "reason": "...",
  "estimated_weeks_since_haircut": null
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
        exc_str = str(exc)
        if any(code in exc_str for code in ("503", "UNAVAILABLE", "429", "RESOURCE_EXHAUSTED")):
            return {
                "error": True,
                "error_type": "service_unavailable",
                "error_message": "Serviciul de analiză este momentan supraîncărcat. Te rugăm să încerci din nou în câteva momente.",
            }
        return _fallback_response(f"Analysis unavailable: {exc}")


async def fetch_image_from_url(url: str) -> tuple[bytes, str]:
    """
    Download an image from the trusted backend.

    Accepts either a relative path (/profile-images/...) or an absolute URL.
    In both cases only the path portion is used and the host is always the
    configured backend base URL — user-supplied host values are discarded.

    Returns (raw_bytes, mime_type).
    Raises httpx.HTTPError on failure.
    """
    parsed = urllib.parse.urlparse(url)
    # Extract only the path (+ query + fragment) — discard any user-supplied host
    path = parsed.path or "/"
    if parsed.query:
        path = path + "?" + parsed.query

    backend_base = settings.dotnet_api_url.rstrip("/")
    safe_url = backend_base + path

    async with httpx.AsyncClient(timeout=15) as client:
        response = await client.get(safe_url)
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
