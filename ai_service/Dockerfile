# ── Build stage ──────────────────────────────────────────────────────────────
FROM python:3.12-slim AS builder

WORKDIR /app

RUN apt-get update \
 && apt-get install -y --no-install-recommends gcc \
 && rm -rf /var/lib/apt/lists/*

COPY requirements.txt .
RUN pip install --no-cache-dir --prefix=/install -r requirements.txt

# ── Runtime stage ─────────────────────────────────────────────────────────────
FROM python:3.12-slim

WORKDIR /app

# Create non-root user to avoid running as root
RUN addgroup --system appuser && adduser --system --ingroup appuser appuser

# Copy installed packages from build stage
COPY --from=builder /install /usr/local

# Copy only the required source files (avoid COPY . . to prevent exposing sensitive files)
COPY main.py config.py ./
COPY models/ models/
COPY routers/ routers/
COPY services/ services/

# Create data directory for calendar token storage
RUN mkdir -p /app/data && chown -R appuser:appuser /app

ENV PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1

USER appuser

EXPOSE 8000

CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000"]
