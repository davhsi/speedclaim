"""Customer-assistant capabilities with explicit, auditable contracts.

This package is intentionally transport-neutral. A future MCP adapter can expose
the same capabilities without changing LangGraph workflows or domain controls.
"""

from speedclaim_ai.tools.catalog import (
    CUSTOMER_TOOL_CATALOG,
    CustomerToolDefinition,
    ToolKind,
    ToolSurface,
    customer_tool,
    customer_tools_for,
)
from speedclaim_ai.tools.customer import CustomerAssistantTools, ToolExecution

__all__ = [
    "CUSTOMER_TOOL_CATALOG",
    "CustomerAssistantTools",
    "CustomerToolDefinition",
    "ToolExecution",
    "ToolKind",
    "ToolSurface",
    "customer_tool",
    "customer_tools_for",
]
