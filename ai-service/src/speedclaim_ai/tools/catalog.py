"""Canonical exposure policy for SpeedClaim's customer-assistant tools.

This module deliberately describes *visibility*, not authorization. The .NET API
remains responsible for authentication, ownership checks, workflow rules, and
audit logging when a transport adapter invokes one of these capabilities.
"""

from dataclasses import dataclass
from enum import StrEnum


class ToolKind(StrEnum):
    READ = "read"
    PREPARE = "prepare"


class ToolSurface(StrEnum):
    INTERNAL = "internal"
    EXTERNAL = "external"


@dataclass(frozen=True)
class CustomerToolDefinition:
    name: str
    kind: ToolKind
    description: str

    @property
    def externally_exposable(self) -> bool:
        """External hosts receive only non-mutating, read-only capabilities."""
        return self.kind is ToolKind.READ


CUSTOMER_TOOL_CATALOG: tuple[CustomerToolDefinition, ...] = (
    CustomerToolDefinition("get_available_products", ToolKind.READ, "List the published SpeedClaim product catalog."),
    CustomerToolDefinition("select_published_brochure", ToolKind.READ, "Select published brochure metadata for a product."),
    CustomerToolDefinition("get_my_kyc_next_step", ToolKind.READ, "Return the customer's KYC workflow state without identity data."),
    CustomerToolDefinition("get_my_policy_summary", ToolKind.READ, "Return the customer's policy summaries."),
    CustomerToolDefinition("get_my_proposal_status", ToolKind.READ, "Return the customer's proposal statuses."),
    CustomerToolDefinition("get_my_next_premium_due", ToolKind.READ, "Return the customer's upcoming premium schedule."),
    CustomerToolDefinition("get_my_claim_status", ToolKind.READ, "Return the customer's claim statuses."),
    CustomerToolDefinition("get_my_grievance_status", ToolKind.READ, "Return the customer's grievance statuses."),
    CustomerToolDefinition("get_customer_assistance", ToolKind.READ, "Return generic customer-assistance facts."),
    CustomerToolDefinition("prepare_quote", ToolKind.PREPARE, "Prepare an indicative quote journey without submitting an application."),
    CustomerToolDefinition("prepare_claim_draft", ToolKind.PREPARE, "Prepare a claim draft without filing it."),
    CustomerToolDefinition("prepare_grievance_draft", ToolKind.PREPARE, "Prepare a grievance draft without filing it."),
)

_BY_NAME = {tool.name: tool for tool in CUSTOMER_TOOL_CATALOG}


def customer_tools_for(surface: ToolSurface) -> tuple[CustomerToolDefinition, ...]:
    """Return the explicit capability list advertised on one MCP surface."""
    if surface is ToolSurface.INTERNAL:
        return CUSTOMER_TOOL_CATALOG
    return tuple(tool for tool in CUSTOMER_TOOL_CATALOG if tool.externally_exposable)


def customer_tool(name: str) -> CustomerToolDefinition:
    """Resolve an emitted tool call and fail closed for an undeclared capability."""
    try:
        return _BY_NAME[name]
    except KeyError as error:
        raise ValueError(f"Unknown customer-assistant tool: {name}") from error
