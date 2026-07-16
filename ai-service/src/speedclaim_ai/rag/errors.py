from dataclasses import dataclass


@dataclass(slots=True)
class IngestionFailure(Exception):
    code: str
    message: str
    status_code: int = 422

    def __str__(self) -> str:
        return self.message


@dataclass(slots=True)
class PolicyQaFailure(Exception):
    code: str
    message: str
    status_code: int = 503
    retry_after_seconds: int | None = None

    def __str__(self) -> str:
        return self.message
