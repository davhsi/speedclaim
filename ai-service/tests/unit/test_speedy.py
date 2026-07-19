from datetime import date
from uuid import uuid4

import pytest

from speedclaim_ai.contracts.speedy import (
    SpeedyAccountSnapshot,
    SpeedyCatalogSnapshot,
    SpeedyClaimSnapshot,
    SpeedyKycSnapshot,
    SpeedyPolicySnapshot,
    SpeedyPremiumSnapshot,
    SpeedyRequest,
)
from speedclaim_ai.providers.chat.base import ChatCompletion
from speedclaim_ai.speedy import SpeedyService


class FakeChatProvider:
    provider_name = "Fake"
    model_name = "fake-model"

    async def complete(self, request):
        assert "ACCOUNT_DATA" in request.user_prompt
        assert "CATALOG_DATA" in request.user_prompt
        assert "What policies" in request.user_prompt
        return ChatCompletion(
            content='{"answer":"You have one active policy."}',
            provider="Fake",
            model="fake-model",
        )

    async def close(self):
        return None


pytestmark = pytest.mark.anyio


def anyio_backend() -> str:
    return "asyncio"


async def test_speedy_uses_only_the_server_supplied_account_snapshot():
    request = SpeedyRequest(
        requestId=uuid4(),
        question="What policies do I have?",
        account=SpeedyAccountSnapshot(firstName="Asha", isAuthenticated=True, policies=[], upcomingPremiums=[], claims=[]),
        catalog=SpeedyCatalogSnapshot(products=[]),
    )

    response = await SpeedyService(FakeChatProvider()).answer(request)

    assert response.request_id == request.request_id
    assert response.answer == "You have one active policy."
    assert response.provider == "Fake"


async def test_speedy_returns_the_same_deterministic_kyc_status_as_the_workspace():
    request = SpeedyRequest(
        requestId=uuid4(),
        question="What is my KYC status?",
        account=SpeedyAccountSnapshot(
            firstName="Asha",
            isAuthenticated=True,
            policies=[],
            upcomingPremiums=[],
            claims=[],
            kyc=SpeedyKycSnapshot(status="Pending", aadhaarUploaded=True, panUploaded=True),
        ),
        catalog=SpeedyCatalogSnapshot(products=[]),
    )

    response = await SpeedyService(FakeChatProvider()).answer(request)

    assert "⏳" in response.answer
    assert "awaiting underwriter review" in response.answer
    assert response.model == "kyc-status-workflow"


def test_speedy_snapshot_accepts_dotnet_datetime_values_from_imported_data():
    policy = SpeedyPolicySnapshot.model_validate(
        {
            "policyNumber": "POL-100",
            "productName": "Family Shield",
            "status": "Active",
            "coverageAmount": 500000,
            "premiumAmount": 1800,
            "paymentFrequency": "MONTHLY",
            "endDate": "2027-07-13T18:30:00Z",
        }
    )
    premium = SpeedyPremiumSnapshot.model_validate(
        {
            "policyNumber": "POL-100",
            "amount": 1800,
            "dueDate": "2026-08-13T18:30:00+05:30",
            "status": "Upcoming",
        }
    )
    claim = SpeedyClaimSnapshot.model_validate(
        {
            "claimNumber": "CLM-100",
            "policyNumber": "POL-100",
            "status": "Settled",
            "intimationDate": "2026-07-13T12:15:00",
        }
    )

    assert policy.end_date == date(2027, 7, 13)
    assert premium.due_date == date(2026, 8, 13)
    assert claim.intimation_date == date(2026, 7, 13)
