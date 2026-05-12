import logging

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from config import settings
from routers import calendar, recommend, haircut

logging.basicConfig(
    level=logging.DEBUG if settings.debug else logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
)

app = FastAPI(
    title=settings.app_name,
    description=(
        "AI-powered appointment recommendation service for SmartCutScheduler. "
        "Uses a hybrid deterministic scoring engine + Google Gemini to suggest "
        "the best available barber slot for each user."
    ),
    version="1.0.0",
    docs_url="/docs",
    redoc_url="/redoc",
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=settings.all_cors_origins,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(recommend.router)
app.include_router(calendar.router)
app.include_router(haircut.router)


@app.get("/health", tags=["health"], summary="Health check")
async def health() -> dict:
    return {"status": "ok", "service": settings.app_name}
