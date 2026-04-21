import os
from pathlib import Path
from pydantic_settings import BaseSettings, SettingsConfigDict

# Resolve .env relative to this file so the server works from any CWD
_ENV_FILE = Path(__file__).parent / ".env"


class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_file=str(_ENV_FILE),
        env_file_encoding="utf-8",
        extra="ignore",
    )

    # .NET Backend API
    dotnet_api_url: str = "http://api:5000"
    dotnet_api_timeout: int = 10

    # Google Gemini
    gemini_api_key: str = ""
    gemini_model: str = "gemini-flash-lite-latest"

    # OpenWeatherMap
    weather_api_key: str = ""
    weather_api_url: str = "https://api.openweathermap.org/data/2.5"
    default_city: str = "Bucharest"

    # Google Calendar OAuth2
    google_client_id: str = ""
    google_client_secret: str = ""
    google_redirect_uri: str = "http://localhost:8000/calendar/callback"

    # Token storage — swap for Redis/DB in production
    token_storage_path: str = "./data/calendar_tokens.json"

    # Scoring weights (configurable via env vars)
    weight_barber_rating: float = 25.0
    weight_preferred_time: float = 30.0
    weight_weather: float = 20.0
    weight_calendar: float = 15.0
    weight_urgency: float = 10.0

    # App
    app_name: str = "SmartCutScheduler AI"
    debug: bool = False
    cors_origins: list[str] = [
        "http://localhost:5177",
        "http://localhost:3000",
        "http://localhost:5173",
    ]


settings = Settings()
