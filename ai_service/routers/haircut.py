"""
Haircut AI router.

Flow:
  1. Frontend uploads a fresh-cut reference photo → stored in the .NET backend
     (this router just validates and proxies if needed; storage is handled by .NET).
  2. User uploads their current photo → sent here along with the stored reference
     photo URL.
  3. This endpoint calls Gemini Vision to compare the two images.
  4. If a haircut is needed the response also includes a friendly recommendation
     message so the frontend can nudge the user to book an appointment.
"""

import logging
from typing import Annotated, Optional

import httpx
from fastapi import APIRouter, File, Form, HTTPException, UploadFile

from models.schemas import HaircutAnalysisResponse
from services.hair_analysis_service import (
    analyze_hair,
    build_recommendation_message,
    fetch_image_from_url,
)

logger = logging.getLogger(__name__)

router = APIRouter(prefix="/haircut", tags=["haircut-ai"])

# Allowed MIME types for uploaded images
_ALLOWED_MIME = {"image/jpeg", "image/png", "image/webp"}
# 10 MB upload limit
_MAX_BYTES = 10 * 1024 * 1024


# ---------------------------------------------------------------------------
# Helper
# ---------------------------------------------------------------------------

async def _read_upload(file: UploadFile) -> tuple[bytes, str]:
    """Read and validate an uploaded image file."""
    content_type = file.content_type or "image/jpeg"
    if content_type not in _ALLOWED_MIME:
        raise HTTPException(
            status_code=415,
            detail=f"Unsupported image type '{content_type}'. Use JPEG, PNG, or WebP.",
        )
    data = await file.read()
    if len(data) > _MAX_BYTES:
        raise HTTPException(status_code=413, detail="Image too large (max 10 MB).")
    if not data:
        raise HTTPException(status_code=400, detail="Uploaded file is empty.")
    return data, content_type


# ---------------------------------------------------------------------------
# Endpoints
# ---------------------------------------------------------------------------

@router.post(
    "/analyze",
    summary="Analyze whether the user needs a haircut",
    description=(
        "Compare the user's current photo against their saved fresh-haircut "
        "reference photo using Gemini Vision. "
        "Provide the reference either as a URL (`reference_photo_url`) "
        "**or** as a direct file upload (`reference_photo`). "
        "The current photo must always be uploaded as a file."
    ),
    responses={
        400: {"description": "Missing reference photo input or empty file"},
        413: {"description": "Uploaded image exceeds 10 MB limit"},
        415: {"description": "Unsupported image type; use JPEG, PNG, or WebP"},
        502: {"description": "Could not fetch reference photo from backend"},
    },
)
async def analyze_haircut(
    current_photo: Annotated[UploadFile, File(description="Current photo of the user (taken today).")],
    reference_photo_url: Annotated[
        Optional[str],
        Form(description="URL of the saved fresh-haircut reference photo (returned by the .NET backend)."),
    ] = None,
    reference_photo: Annotated[
        Optional[UploadFile],
        File(description="Alternative: upload the reference photo directly."),
    ] = None,
) -> HaircutAnalysisResponse:
    """
    Main haircut-detection endpoint.

    Accepts the user's current photo plus a reference (fresh-cut) photo,
    calls Gemini Vision for comparison, and returns an analysis result.
    """
    # ── Validate inputs ────────────────────────────────────────────────────
    if reference_photo_url is None and reference_photo is None:
        raise HTTPException(
            status_code=400,
            detail="Provide either 'reference_photo_url' or 'reference_photo'.",
        )

    # ── Read current photo ─────────────────────────────────────────────────
    current_bytes, current_mime = await _read_upload(current_photo)

    # ── Read reference photo ───────────────────────────────────────────────
    if reference_photo is not None:
        reference_bytes, reference_mime = await _read_upload(reference_photo)
    else:
        try:
            reference_bytes, reference_mime = await fetch_image_from_url(reference_photo_url)  # type: ignore[arg-type]
        except httpx.HTTPStatusError as exc:
            raise HTTPException(
                status_code=502,
                detail=f"Could not fetch reference photo from backend: HTTP {exc.response.status_code}",
            ) from exc
        except Exception as exc:
            logger.error("Failed to fetch reference photo: %s", exc)
            raise HTTPException(
                status_code=502,
                detail="Could not retrieve the reference photo. Please try again.",
            ) from exc

    # ── Call Gemini Vision ─────────────────────────────────────────────────
    analysis = await analyze_hair(
        reference_bytes=reference_bytes,
        current_bytes=current_bytes,
        reference_mime=reference_mime,
        current_mime=current_mime,
    )

    # ── Propagate validation errors from Gemini ────────────────────────────
    if analysis.get("error"):
        return HaircutAnalysisResponse(
            error=True,
            error_type=analysis.get("error_type"),
            error_message=analysis.get("error_message"),
        )

    confidence: float = float(analysis.get("confidence", 0.0))
    growth_level: str = str(analysis.get("hair_growth_level", "unknown"))
    # Require confidence >= 0.60 before declaring a haircut is needed.
    # Below this threshold the comparison is too ambiguous to act on.
    _raw_needs_haircut: bool = bool(analysis.get("needs_haircut", False))
    needs_haircut: bool = _raw_needs_haircut and confidence >= 0.60
    reason: str = str(analysis.get("reason", ""))
    raw_weeks = analysis.get("estimated_weeks_since_haircut")
    estimated_weeks: Optional[int] = None
    if raw_weeks is not None:
        try:
            # Guard against Gemini returning a string like "3-5 weeks" instead of an int
            estimated_weeks = int(str(raw_weeks).split("-")[0].split("–")[0].strip().split()[0])
        except (ValueError, IndexError):
            estimated_weeks = None

    # ── Build recommendation message when haircut is needed ────────────────
    recommendation_message: Optional[str] = None
    if needs_haircut:
        recommendation_message = build_recommendation_message(growth_level, estimated_weeks)

    return HaircutAnalysisResponse(
        needs_haircut=needs_haircut,
        confidence=confidence,
        hair_growth_level=growth_level,
        reason=reason,
        estimated_weeks_since_haircut=estimated_weeks,
        recommendation_message=recommendation_message,
    )
