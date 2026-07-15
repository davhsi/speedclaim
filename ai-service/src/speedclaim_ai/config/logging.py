import contextvars
import json
import logging
import re
import sys
from datetime import UTC, datetime
from typing import Any

from pydantic import SecretStr

correlation_id_context: contextvars.ContextVar[str | None] = contextvars.ContextVar(
    "correlation_id",
    default=None,
)

_REDACTED = "[REDACTED]"
_SENSITIVE_KEY_SUFFIXES = (
    "authorization",
    "apikey",
    "connectionstring",
    "password",
    "secret",
    "token",
    "question",
    "answer",
    "excerpt",
    "content",
    "prompt",
    "brochuretext",
    "chunktext",
    "documenttext",
    "requestbody",
    "responsebody",
)
_LABELED_SECRET = re.compile(
    r"(?i)\b(authorization|x-internal-api-key|api[_-]?key|password|secret|token)"
    r"(\s*[:=]\s*)((?:bearer\s+)?[^\s,;]+)"
)
_STANDARD_LOG_RECORD_FIELDS = frozenset(logging.makeLogRecord({}).__dict__) | {
    "asctime",
    "message",
}


def _is_sensitive_key(key: object) -> bool:
    normalized = re.sub(r"[^a-z0-9]", "", str(key).lower())
    return any(normalized.endswith(suffix) for suffix in _SENSITIVE_KEY_SUFFIXES)


def _redact_text(value: str) -> str:
    return _LABELED_SECRET.sub(lambda match: f"{match.group(1)}{match.group(2)}{_REDACTED}", value)


def redact(value: Any, key: object | None = None) -> Any:
    if key is not None and _is_sensitive_key(key):
        return _REDACTED
    if isinstance(value, SecretStr):
        return _REDACTED
    if isinstance(value, dict):
        return {str(item_key): redact(item_value, item_key) for item_key, item_value in value.items()}
    if isinstance(value, (list, tuple, set, frozenset)):
        return [redact(item) for item in value]
    if isinstance(value, str):
        return _redact_text(value)
    if value is None or isinstance(value, (bool, int, float)):
        return value
    return str(value)


class RedactingJsonFormatter(logging.Formatter):
    def format(self, record: logging.LogRecord) -> str:
        payload: dict[str, Any] = {
            "timestamp": datetime.now(UTC).isoformat().replace("+00:00", "Z"),
            "level": record.levelname,
            "logger": record.name,
            "message": _redact_text(record.getMessage()),
        }
        correlation_id = correlation_id_context.get()
        if correlation_id:
            payload["correlationId"] = correlation_id

        for key, value in record.__dict__.items():
            if key not in _STANDARD_LOG_RECORD_FIELDS:
                payload[key] = redact(value, key)

        if record.exc_info and record.exc_info[0]:
            payload["exceptionType"] = record.exc_info[0].__name__

        return json.dumps(payload, separators=(",", ":"), ensure_ascii=False)


def configure_logging(level: str) -> None:
    handler = logging.StreamHandler(sys.stdout)
    handler.setFormatter(RedactingJsonFormatter())

    root_logger = logging.getLogger()
    root_logger.handlers.clear()
    root_logger.addHandler(handler)
    root_logger.setLevel(level)

    # Uvicorn access logs include the raw target, including query strings. The service emits
    # its own bounded path-only request event instead.
    access_logger = logging.getLogger("uvicorn.access")
    access_logger.handlers.clear()
    access_logger.propagate = False
    access_logger.disabled = True

    for logger_name in ("uvicorn", "uvicorn.error", "fastapi"):
        logger = logging.getLogger(logger_name)
        logger.handlers.clear()
        logger.propagate = True
