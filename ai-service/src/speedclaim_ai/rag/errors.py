from dataclasses import dataclass


@dataclass(slots=True)
class IngestionFailure(Exception):
    code: str
    message: str
    status_code: int = 422

    def __str__(self) -> str:
        return self.message
