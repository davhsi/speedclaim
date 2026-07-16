from typing import Protocol


class BrochureReader(Protocol):
    async def read_bytes(self, blob_path: str, *, max_bytes: int) -> bytes: ...
