from speedclaim_ai.tools.catalog import ToolKind, ToolSurface, customer_tool, customer_tools_for


def test_external_catalog_is_a_strict_read_only_subset_of_the_internal_catalog():
    internal = customer_tools_for(ToolSurface.INTERNAL)
    external = customer_tools_for(ToolSurface.EXTERNAL)

    assert external
    assert {tool.name for tool in external} < {tool.name for tool in internal}
    assert all(tool.kind is ToolKind.READ for tool in external)


def test_internal_catalog_retains_non_committing_prepare_capabilities_only():
    internal = customer_tools_for(ToolSurface.INTERNAL)

    assert {tool.name for tool in internal if tool.kind is ToolKind.PREPARE} == {
        "prepare_quote",
        "prepare_claim_draft",
        "prepare_grievance_draft",
    }
    assert all("payment" not in tool.name and "submit" not in tool.name for tool in internal)


def test_catalog_fails_closed_for_undeclared_tool_names():
    assert customer_tool("get_my_policy_summary").kind is ToolKind.READ

    try:
        customer_tool("settle_claim")
    except ValueError as error:
        assert "Unknown customer-assistant tool" in str(error)
    else:
        raise AssertionError("Unknown tools must not be silently exposed")
