from typing import Literal

from pydantic import BaseModel, ConfigDict, Field

from speedclaim_ai.contracts.speedy import SpeedyAccountSnapshot, SpeedyCatalogSnapshot


class WorkspaceRequest(BaseModel):
    """A tenant-scoped, server-supplied snapshot for the customer workspace."""

    model_config = ConfigDict(populate_by_name=True)

    request_id: str = Field(alias="requestId", min_length=1, max_length=64)
    question: str = Field(min_length=1, max_length=2_000)
    account: SpeedyAccountSnapshot
    catalog: SpeedyCatalogSnapshot


class WorkspaceAction(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    kind: Literal[
        "navigate", "guided_kyc", "guided_quote", "guided_application",
        "guided_claim", "claim_status", "policy_status", "claim_documents", "payment", "none",
    ]
    label: str = Field(min_length=1, max_length=80)
    route: str | None = Field(default=None, max_length=160)
    detail: str = Field(min_length=1, max_length=300)
    requires_confirmation: bool = Field(alias="requiresConfirmation", default=False)


class WorkspaceResponse(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    request_id: str = Field(alias="requestId")
    answer: str = Field(min_length=1, max_length=8_000)
    intent: str = Field(min_length=1, max_length=64)
    risk: Literal["low", "regulated"]
    actions: list[WorkspaceAction] = Field(default_factory=list, max_length=3)
    provider: str | None = None
    model: str | None = None
