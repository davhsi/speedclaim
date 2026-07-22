# Customer external-AI account linking

## Purpose

Customers may let an approved external AI host use SpeedClaim's read-only MCP tools. The host's
Auth0 identity must be connected to exactly one SpeedClaim customer account without putting a
linking code, password, SpeedClaim JWT, or customer identifier in an AI chat.

## Customer journey

1. The customer signs in to the SpeedClaim portal and opens **Profile → Connected apps**.
2. They select **Connect external AI**.
3. The API creates a short-lived, opaque state record in the server cache with the SpeedClaim user
   id and a PKCE verifier, then opens Auth0 in the browser.
4. The customer signs in to Auth0 and Auth0 returns only an authorization code to the API callback.
5. The API exchanges that code server-side, calls Auth0 UserInfo to obtain the verified `sub`, and
   persists the `Auth0` subject-to-SpeedClaim-user mapping.
6. The customer returns to Profile with a Connected status. Claude, ChatGPT, or another host can
   subsequently resolve that same Auth0 subject for read-only account tools.

The state transaction expires after 10 minutes and is removed after the callback is attempted.

## Auth0 application setup

Create a separate **Regular Web Application** named `SpeedClaim Account Linking`.

| Setting | Value |
| --- | --- |
| Allowed Callback URLs | `https://20.235.26.238.sslip.io/api/v1/users/external-identities/auth0/callback` |
| Allowed Logout URLs | Your normal SpeedClaim frontend URL (optional for this flow) |
| Grant type | Authorization Code with PKCE |

Store the application credentials in Azure Key Vault, never in Git:

| Key Vault secret | Value |
| --- | --- |
| `Mcp--External--AccountLinkClientId` | Auth0 application Client ID |
| `Mcp--External--AccountLinkClientSecret` | Auth0 application Client Secret |

`Mcp--External--Issuer` and `Mcp--External--PublicBaseUrl` remain the values already used by the
MCP OAuth configuration. Restart the API deployment after adding or changing these Key Vault
secrets because configuration is loaded at startup.

## Security boundaries

- Auth0 subjects are linked only to the already authenticated, active, verified SpeedClaim customer
  that initiated the browser transaction.
- A subject cannot be linked to a second customer, and a customer cannot be linked to a different
  Auth0 subject.
- The external MCP server exposes read-only tools only; it does not expose identity linking,
  payments, claim submissions, KYC uploads, applications, or status changes.
- The Auth0 client secret is used only by the API's server-side token exchange and is never sent to
  the Angular application or an AI host.
