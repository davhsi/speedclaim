"""Customer-assistant capabilities with explicit, auditable contracts.

This package is intentionally transport-neutral. A future MCP adapter can expose
the same capabilities without changing LangGraph workflows or domain controls.
"""

from speedclaim_ai.tools.customer import CustomerAssistantTools, ToolExecution

__all__ = ["CustomerAssistantTools", "ToolExecution"]
