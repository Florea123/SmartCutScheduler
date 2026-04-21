import logging

import httpx

from config import settings

logger = logging.getLogger(__name__)

# Map OpenWeatherMap condition IDs → (score 0–20, label)
_WEATHER_MAP: list[tuple[range, float, str]] = [
    (range(200, 233), 2.0,  "thunderstorm"),
    (range(300, 322), 10.0, "drizzle"),
    (range(500, 532), 5.0,  "rain"),
    (range(600, 623), 8.0,  "snow"),
    (range(700, 782), 12.0, "fog / mist"),
    (range(800, 801), 20.0, "clear sky"),
    (range(801, 803), 18.0, "few / scattered clouds"),
    (range(803, 805), 14.0, "broken / overcast clouds"),
]


def _condition_to_score(condition_id: int) -> tuple[float, str]:
    for id_range, score, label in _WEATHER_MAP:
        if condition_id in id_range:
            return score, label
    return 14.0, "unknown"


async def get_weather_score(city: str) -> tuple[float, str]:
    """
    Fetch current weather for *city* from OpenWeatherMap.
    Returns (score 0–20, weather description).
    Falls back to 14.0 (neutral "cloudy") on any error or missing API key.
    """
    if not settings.weather_api_key:
        logger.info("Weather API key not configured — returning neutral score")
        return 14.0, "weather unavailable"

    params = {
        "q": city,
        "appid": settings.weather_api_key,
        "units": "metric",
    }
    try:
        async with httpx.AsyncClient(timeout=5) as client:
            response = await client.get(
                f"{settings.weather_api_url}/weather", params=params
            )
            response.raise_for_status()
            data = response.json()
            condition_id: int = data["weather"][0]["id"]
            score, label = _condition_to_score(condition_id)
            logger.info(
                "Weather in %s: %s (id=%d) → score=%.1f", city, label, condition_id, score
            )
            return score, label
    except httpx.TimeoutException:
        logger.warning("Weather API timeout for city '%s'", city)
    except httpx.HTTPStatusError as exc:
        logger.warning("Weather API HTTP %s for city '%s'", exc.response.status_code, city)
    except Exception as exc:
        logger.error("Weather API error: %s", exc)

    return 14.0, "weather unavailable"
