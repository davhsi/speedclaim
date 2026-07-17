from datetime import date, datetime
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field, field_validator


def _coerce_dotnet_datetime_to_date(value: object) -> object:
    """Accept .NET DateTime JSON while retaining date-only account semantics.

    The API owns the account snapshot and serializes its DateTime fields as ISO
    timestamps.  Those values can legitimately contain a time component after
    data import, whereas Speedy only needs the calendar date to answer a
    customer question.
    """
    if isinstance(value, datetime):
        return value.date()
    if isinstance(value, str) and "T" in value:
        return value.split("T", maxsplit=1)[0]
    return value


class SpeedyPolicySnapshot(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    policy_number: str = Field(alias="policyNumber", min_length=1, max_length=80)
    product_name: str = Field(alias="productName", min_length=1, max_length=160)
    status: str = Field(min_length=1, max_length=40)
    coverage_amount: float = Field(alias="coverageAmount", ge=0)
    premium_amount: float = Field(alias="premiumAmount", ge=0)
    payment_frequency: str = Field(alias="paymentFrequency", min_length=1, max_length=40)
    end_date: date = Field(alias="endDate")

    @field_validator("end_date", mode="before")
    @classmethod
    def coerce_end_date(cls, value: object) -> object:
        return _coerce_dotnet_datetime_to_date(value)


class SpeedyPremiumSnapshot(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    policy_number: str = Field(alias="policyNumber", min_length=1, max_length=80)
    amount: float = Field(ge=0)
    due_date: date = Field(alias="dueDate")
    status: str = Field(min_length=1, max_length=40)

    @field_validator("due_date", mode="before")
    @classmethod
    def coerce_due_date(cls, value: object) -> object:
        return _coerce_dotnet_datetime_to_date(value)


class SpeedyClaimSnapshot(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    claim_number: str = Field(alias="claimNumber", min_length=1, max_length=80)
    policy_number: str = Field(alias="policyNumber", min_length=1, max_length=80)
    status: str = Field(min_length=1, max_length=40)
    intimation_date: date = Field(alias="intimationDate")

    @field_validator("intimation_date", mode="before")
    @classmethod
    def coerce_intimation_date(cls, value: object) -> object:
        return _coerce_dotnet_datetime_to_date(value)


class SpeedyAccountSnapshot(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    first_name: str = Field(alias="firstName", min_length=1, max_length=80)
    policies: list[SpeedyPolicySnapshot] = Field(default_factory=list, max_length=20)
    upcoming_premiums: list[SpeedyPremiumSnapshot] = Field(alias="upcomingPremiums", default_factory=list, max_length=5)
    claims: list[SpeedyClaimSnapshot] = Field(default_factory=list, max_length=5)


class SpeedyRequest(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    request_id: UUID = Field(alias="requestId")
    question: str = Field(min_length=1, max_length=2_000)
    account: SpeedyAccountSnapshot

    @field_validator("question")
    @classmethod
    def validate_question(cls, value: str) -> str:
        if value != value.strip():
            raise ValueError("question must not contain surrounding whitespace")
        return value


class SpeedyResponse(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    request_id: UUID = Field(alias="requestId")
    answer: str = Field(min_length=1, max_length=1_600)
    provider: str | None = None
    model: str | None = None
