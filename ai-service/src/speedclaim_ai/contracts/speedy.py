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


class SpeedyProposalSnapshot(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    proposal_number: str = Field(alias="proposalNumber", min_length=1, max_length=80)
    product_name: str = Field(alias="productName", min_length=1, max_length=160)
    status: str = Field(min_length=1, max_length=40)
    submitted_at: datetime = Field(alias="submittedAt")


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


class SpeedyKycSnapshot(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    status: str = Field(min_length=1, max_length=40)
    aadhaar_uploaded: bool = Field(alias="aadhaarUploaded")
    pan_uploaded: bool = Field(alias="panUploaded")


class SpeedyGrievanceSnapshot(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    grievance_number: str = Field(alias="grievanceNumber", min_length=1, max_length=80)
    category: str = Field(min_length=1, max_length=80)
    status: str = Field(min_length=1, max_length=40)
    created_at: datetime = Field(alias="createdAt")
    resolved_at: datetime | None = Field(alias="resolvedAt", default=None)


class SpeedyAccountSnapshot(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    first_name: str = Field(alias="firstName", min_length=1, max_length=80)
    is_authenticated: bool = Field(alias="isAuthenticated", default=False)
    proposals: list[SpeedyProposalSnapshot] = Field(default_factory=list, max_length=10)
    policies: list[SpeedyPolicySnapshot] = Field(default_factory=list, max_length=20)
    upcoming_premiums: list[SpeedyPremiumSnapshot] = Field(alias="upcomingPremiums", default_factory=list, max_length=5)
    claims: list[SpeedyClaimSnapshot] = Field(default_factory=list, max_length=5)
    grievances: list[SpeedyGrievanceSnapshot] = Field(default_factory=list, max_length=5)
    kyc: SpeedyKycSnapshot | None = None


class SpeedyProductSnapshot(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    product_name: str = Field(alias="productName", min_length=1, max_length=160)
    domain: str = Field(min_length=1, max_length=40)
    description: str = Field(min_length=1, max_length=4_000)
    min_age: int = Field(alias="minAge", ge=0, le=120)
    max_age: int = Field(alias="maxAge", ge=0, le=120)
    min_sum_assured: float = Field(alias="minSumAssured", ge=0)
    max_sum_assured: float = Field(alias="maxSumAssured", ge=0)
    min_tenure_years: int = Field(alias="minTenureYears", ge=0)
    max_tenure_years: int = Field(alias="maxTenureYears", ge=0)
    waiting_period_days: int = Field(alias="waitingPeriodDays", ge=0)
    allows_family_floater: bool = Field(alias="allowsFamilyFloater")
    max_family_members: int = Field(alias="maxFamilyMembers", ge=0)
    motor_vehicle_type: str | None = Field(alias="motorVehicleType", default=None, max_length=100)


class SpeedyCatalogSnapshot(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    products: list[SpeedyProductSnapshot] = Field(default_factory=list, max_length=50)


class SpeedyRequest(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    request_id: UUID = Field(alias="requestId")
    question: str = Field(min_length=1, max_length=2_000)
    account: SpeedyAccountSnapshot
    catalog: SpeedyCatalogSnapshot

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
