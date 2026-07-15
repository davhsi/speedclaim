import json
import logging
import sys

from speedclaim_ai.config.logging import RedactingJsonFormatter, correlation_id_context


def test_formatter_emits_json_and_recursively_redacts_sensitive_fields() -> None:
    secret = "speedclaim-secret-value"
    record = logging.makeLogRecord(
        {
            "name": "speedclaim.test",
            "levelno": logging.INFO,
            "levelname": "INFO",
            "msg": "Authorization: Bearer bearer-secret-value event completed",
            "args": (),
            "apiKey": secret,
            "payload": {
                "question": "What is covered?",
                "content": "raw brochure clause",
                "nested": {"password": "password-value"},
                "safe": "brochure-123",
                "inputTokenCount": 42,
            },
        }
    )
    token = correlation_id_context.set("correlation-123")
    try:
        output = RedactingJsonFormatter().format(record)
    finally:
        correlation_id_context.reset(token)

    payload = json.loads(output)
    assert payload["correlationId"] == "correlation-123"
    assert payload["apiKey"] == "[REDACTED]"
    assert payload["payload"]["question"] == "[REDACTED]"
    assert payload["payload"]["content"] == "[REDACTED]"
    assert payload["payload"]["nested"]["password"] == "[REDACTED]"
    assert payload["payload"]["safe"] == "brochure-123"
    assert payload["payload"]["inputTokenCount"] == 42
    assert "bearer-secret-value" not in output
    assert secret not in output


def test_formatter_logs_exception_type_without_exception_message() -> None:
    secret = "raw-question-or-secret"
    try:
        raise RuntimeError(secret)
    except RuntimeError:
        record = logging.makeLogRecord(
            {
                "name": "speedclaim.test",
                "levelno": logging.ERROR,
                "levelname": "ERROR",
                "msg": "Request failed",
                "args": (),
                "exc_info": sys.exc_info(),
            }
        )

    output = RedactingJsonFormatter().format(record)

    assert json.loads(output)["exceptionType"] == "RuntimeError"
    assert secret not in output
