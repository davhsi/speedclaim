"""Safe, read/prepare-only capabilities for the customer assistant.

The trusted .NET API creates ``WorkspaceRequest`` after authenticating the customer
and applying ownership checks. These tools operate only on that server-supplied
projection: they never accept customer identifiers, connect to the business
database, or perform a domain write.
"""

from dataclasses import dataclass
from typing import Any
from uuid import UUID

from speedclaim_ai.contracts.workspace import WorkspaceAction, WorkspaceRequest, WorkspaceToolCall
from speedclaim_ai.kyc_workflow import kyc_is_under_review_or_approved, kyc_status_answer


@dataclass(frozen=True)
class ToolExecution:
    """Result of one internal capability invocation."""

    call: WorkspaceToolCall
    facts: dict[str, Any]
    action: WorkspaceAction | None = None
    deterministic_answer: str | None = None
    brochure_id: UUID | None = None


class CustomerAssistantTools:
    """Registry for the customer-facing, non-committing assistant capabilities."""

    def execute(self, intent: str, request: WorkspaceRequest, brochure_id: UUID | None = None) -> ToolExecution:
        account = request.account
        if intent == "brochure_qa":
            brochure = next((item for item in request.catalog.brochures if item.brochure_id == brochure_id), None)
            return ToolExecution(
                WorkspaceToolCall(name="select_published_brochure", kind="read"),
                {"selectedBrochure": brochure.model_dump(mode="json", by_alias=True) if brochure else None},
                brochure_id=brochure.brochure_id if brochure else None,
            )
        if intent == "kyc":
            return ToolExecution(
                WorkspaceToolCall(name="get_my_kyc_next_step", kind="read"),
                {"kyc": account.kyc.model_dump(mode="json", by_alias=True) if account.kyc else None},
                action=None if kyc_is_under_review_or_approved(account) else _customer_action("kyc", account.is_authenticated),
                deterministic_answer=kyc_status_answer(account),
            )
        if intent == "policy_help":
            return self._account_read("get_my_policy_summary", "policy_help", request)
        if intent == "proposal_status":
            return self._account_read("get_my_proposal_status", "proposal_status", request)
        if intent == "premium_help":
            return self._account_read("get_my_next_premium_due", "premium_help", request)
        if intent == "claim_status":
            return self._account_read("get_my_claim_status", "claim_status", request)
        if intent == "grievance_status":
            return self._account_read("get_my_grievance_status", "grievance_status", request)
        if intent == "proposal" and account.is_authenticated and not kyc_is_under_review_or_approved(account):
            return ToolExecution(
                WorkspaceToolCall(name="get_my_kyc_next_step", kind="read"),
                {"kyc": account.kyc.model_dump(mode="json", by_alias=True) if account.kyc else None},
                action=_customer_action("kyc", True),
                deterministic_answer=(
                    "📋 **Complete KYC first**\n\n"
                    "You can browse products and compare cover now. Before you start a proposal, "
                    "please submit both Aadhaar and PAN in the secure KYC checklist. Once submitted, "
                    "you can return here to build your quote and application."
                ),
            )
        action = _customer_action(intent, account.is_authenticated)
        return ToolExecution(
            WorkspaceToolCall(name=_tool_name_for(intent), kind="prepare" if action else "read"),
            {"products": [item.model_dump(mode="json", by_alias=True) for item in request.catalog.products]},
            action=action,
        )

    @staticmethod
    def _account_read(name: str, intent: str, request: WorkspaceRequest) -> ToolExecution:
        account = request.account
        values = {
            "policy_help": account.policies,
            "proposal_status": account.proposals,
            "premium_help": account.upcoming_premiums,
            "claim_status": account.claims,
            "grievance_status": account.grievances,
        }[intent]
        return ToolExecution(
            WorkspaceToolCall(name=name, kind="read"),
            {"items": [item.model_dump(mode="json", by_alias=True) for item in values]},
            action=_customer_action(intent, account.is_authenticated),
        )


def _tool_name_for(intent: str) -> str:
    return {
        "product_discovery": "get_available_products",
        "proposal": "prepare_quote",
        "claim_guidance": "prepare_claim_draft",
        "grievance": "prepare_grievance_draft",
    }.get(intent, "get_customer_assistance")


def _customer_action(intent: str, is_authenticated: bool) -> WorkspaceAction | None:
    public_actions: dict[str, WorkspaceAction] = {
        "product_discovery": WorkspaceAction(kind="guided_quote", label="Build a quote", route=None, detail="Choose a product and review an indicative premium in this workspace.", requiresConfirmation=False),
        "proposal": WorkspaceAction(kind="guided_quote", label="Build a quote", route=None, detail="Choose a product and review an indicative premium in this workspace.", requiresConfirmation=False),
    }
    customer_actions: dict[str, WorkspaceAction] = {
        "policy_help": WorkspaceAction(kind="navigate", label="View my policies", route="/policies", detail="Review your policy details and account documents.", requiresConfirmation=False),
        "proposal_status": WorkspaceAction(kind="policy_status", label="Check application status", route=None, detail="Review your submitted application and policy status in this workspace.", requiresConfirmation=False),
        "premium_help": WorkspaceAction(kind="payment", label="Pay a premium", route=None, detail="Review the next payable installment before opening secure Stripe checkout.", requiresConfirmation=True),
        "claim_guidance": WorkspaceAction(kind="guided_claim", label="Start a claim", route=None, detail="Complete the claim details and explicitly confirm before submitting.", requiresConfirmation=True),
        "claim_status": WorkspaceAction(kind="claim_status", label="Track my claims", route=None, detail="Review the current status and next steps for your claims.", requiresConfirmation=False),
        "kyc": WorkspaceAction(kind="guided_kyc", label="Complete KYC", route=None, detail="Attach Aadhaar and PAN in their labelled slots before continuing.", requiresConfirmation=True),
        "grievance": WorkspaceAction(kind="navigate", label="Raise a grievance", route="/grievances/new", detail="Prepare the grievance and review it before filing.", requiresConfirmation=True),
        "grievance_status": WorkspaceAction(kind="grievance_status", label="Check grievance status", route=None, detail="Review your submitted grievances and their current status.", requiresConfirmation=False),
    }
    if intent in public_actions:
        return public_actions[intent]
    return customer_actions.get(intent) if is_authenticated else None
