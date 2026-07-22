# SpeedClaim MCP integration architecture

## Status and decision

**Status:** the catalog/policy foundation, SpeedClaim-side Auth0 identity-link foundation, and
feature-gated external MCP resource-server implementation are complete. External exposure remains
disabled until the Auth0 production configuration and host verification are complete.
There is no active public MCP endpoint: `POST /mcp` and OAuth protected-resource metadata are only
mapped when `Mcp:External:Enabled=true`; the committed default is `false`.

SpeedClaim will use MCP as an integration boundary for AI hosts, not as a replacement for the Angular-to-API application contract. The existing browser application continues to call the SpeedClaim API directly. This keeps the normal customer journey, JWT/session checks, idempotency, Stripe confirmation, audit trail, and domain workflow ownership unchanged.

The existing `ai-service` customer-tool layer is the canonical capability vocabulary. It is already transport-neutral and operates only on a .NET-authorized, minimal customer snapshot. An MCP adapter may expose that vocabulary; it must not receive database credentials or bypass the .NET API's authorization and domain services.

The .NET application stores an `Auth0` provider subject separately from first-party sessions and
passwords. A verified customer can create a single-use, 10-minute linking code; the feature-gated
MCP adapter consumes that code only after validating an Auth0 token. Email matching is never
used to link identities, and there is no public route that accepts an unverified external subject.

## Target topology

```text
                           +--------------------------------------+
                           | Existing SpeedClaim web application  |
                           | Angular -> authenticated .NET API    |
                           +-------------------+------------------+
                                               |
                                               | Existing API contract (unchanged)
                                               v
 +-----------------+                 +------------------------------+
| ChatGPT / Claude| -- OAuth 2.1 -->| Feature-gated .NET MCP server |
| external clients|                 | read-only catalog only        |
 +-----------------+                 +---------------+--------------+
                                               |
                                               | service-to-service, audience scoped
                                               v
 +-----------------+                 +------------------------------+
 | trusted internal| -- service auth | Planned internal MCP adapter  |
 | AI/orchestration|                 | read + prepare catalog        |
 +-----------------+                 +---------------+--------------+
                                               |
                                               v
                                  +------------------------------+
                                  | .NET API domain services      |
                                  | auth, ownership, audit, writes|
                                  +------------------------------+
```

The two MCP surfaces are separate endpoints/deployments and are independently configured. The public endpoint must never advertise a tool merely because the internal endpoint has it.

## Tool policy

The MCP adapter uses an explicit allowlist. Tool visibility is not an authorization decision; the .NET API must still authorize every invocation against the authenticated SpeedClaim user.

| Tool group | Current capabilities | Internal MCP | External MCP |
| --- | --- | --- | --- |
| Published catalog | `get_available_products`, `select_published_brochure` | Yes | Yes |
| Customer state, read-only | `get_my_policy_summary`, `get_my_proposal_status`, `get_my_next_premium_due`, `get_my_claim_status`, `get_my_grievance_status`, `get_my_kyc_next_step` | Yes | Yes, only after user OAuth and ownership checks |
| Guided preparation | `prepare_quote`, `prepare_claim_draft`, `prepare_grievance_draft` | Yes | No |
| Domain writes | payments, claim submission, KYC upload, proposal submission, status changes, administration | No direct MCP write tool in the first release | Never in the public read-only endpoint |

`link_speedclaim_account` is a narrowly scoped identity-linking step, not a domain-write tool. It consumes a single-use code created by an already signed-in, verified SpeedClaim customer, and binds the current verified Auth0 subject without email matching.

"Prepare" tools can return a draft or a deep link back to SpeedClaim, but they must not make any business mutation. The SpeedClaim UI remains the confirmation surface for payments, claims, KYC, applications, and grievances.

## Authentication and data handling

The current SpeedClaim JWT/session flow is appropriate for the first-party web application, but it is not an OAuth authorization server. Do not expose the existing internal API-key route or ask a user to paste a SpeedClaim JWT into ChatGPT or Claude.

Before an external MCP endpoint is enabled, SpeedClaim must provide or integrate:

1. OAuth 2.1 authorization-code flow with PKCE, dynamic client registration where required by the host, refresh-token rotation, revocation, and consent.
2. MCP protected-resource metadata and a resource-server token verifier. The feature-gated API implementation provides these when enabled.
3. Narrow scopes such as `speedclaim.catalog.read` and `speedclaim.account.read`; no broad `speedclaim.write` scope is part of the external endpoint.
4. Backend token exchange/service authentication that forwards a subject, audience, scopes, correlation ID, and client identity. The MCP server must never use a shared customer identity or database credential.
5. Audit events containing tool name, caller/client ID, subject, scope, decision, correlation ID, and outcome—never policy document text, KYC numbers, payment details, or access tokens.

Tool results remain minimized to the existing customer-visible snapshots. KYC identity values, document paths, internal notes, Stripe secrets, and raw medical/claim attachments are excluded.

## Rollout gates

1. **Catalog and policy tests:** verify the external list contains only read-only tools and the internal list contains only read/prepare tools.
2. **Local inspector:** test the MCP protocol without production data, using fixtures.
3. **OAuth integration test:** prove an unauthenticated request is rejected, a user can see only their data, a revoked/deactivated user is rejected, and a public client cannot invoke a prepare/write capability.
4. **Staging:** run with the external endpoint disabled by default, then enable it only for a test client and test accounts.
5. **Production:** create a separate public hostname and OAuth client registrations only after security review, privacy review, monitoring, incident runbook, and rate limits are in place.

## Reversal

The change is reversible without a data migration. Set `Mcp:External:Enabled=false` and deploy (or revert the release), then revoke its OAuth client registrations if needed. The existing web application and .NET API remain fully functional because they do not depend on MCP.

## Explicit non-goals for the first release

- Exposing every REST endpoint as an MCP tool.
- Allowing an external AI host to execute a payment, submit a claim, upload KYC, or alter a policy or user.
- Moving business rules, authorization, or persistence from .NET into the MCP server.
- Sending a customer's JWT, KYC data, document bytes, payment credentials, or database access to an AI host.
