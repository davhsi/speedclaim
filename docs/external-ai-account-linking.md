# Customer external-AI account linking

## Purpose

Customers may let an approved external AI host use SpeedClaim's read-only MCP tools. The host's
Auth0 identity must be connected to exactly one SpeedClaim customer account without putting a
linking code, password, SpeedClaim JWT, or customer identifier in an AI chat.

## Customer journey

### Preferred: URL-only connector sign-in

1. The customer adds the public MCP URL in Claude, ChatGPT, or another compatible host.
2. The host dynamically registers its public OAuth client, then opens Auth0's hosted sign-in and
   consent page. The customer never handles an OAuth client ID, client secret, token, or code.
3. Auth0 issues a SpeedClaim MCP token containing the two read-only permissions and the
   namespaced verified-email claims configured below.
4. At the first account-specific MCP call, SpeedClaim matches that Auth0-verified email to exactly
   one active, verified **Customer** record and saves the Auth0 subject mapping.
5. Later calls resolve directly from the durable subject mapping.

Automatic matching never guesses. Missing/unverified email claims, staff accounts, inactive
accounts, account conflicts, and no matching customer all fail safely and use the manual fallback.

### Manual fallback: Profile → Connected apps

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

### Canonical resource URL — required before enabling the endpoint

The OAuth resource, the Auth0 API identifier, and the MCP transport URL must be the **same
canonical URL**. For the current environment, that value is:

```text
https://20.235.26.238.sslip.io/mcp
```

This is intentionally different from the public API origin. An OAuth host such as Claude caches
credentials by resource URL. Configuring Auth0 with the origin (`https://20.235.26.238.sslip.io/`)
while the host connects to `/mcp` makes the host treat setup and tool use as two separate protected
resources, which produces a repeated **Connect** prompt.

In Auth0, create a new API (the existing identifier cannot be edited) such as **SpeedClaim MCP API
– Claude Development v3** with this exact Identifier. Add only these permissions and enable RBAC
plus **Add Permissions in the Access Token**:

```text
speedclaim.catalog.read
speedclaim.account.read
```

For that API's **Settings → Default Permissions for third-party applications**, set
**User-delegated Access** to **Authorized — Pick and choose permissions**, then select both
permissions. This permits dynamically registered Claude clients to request them.

Update these Azure Key Vault values before deploying the matching backend release:

| Key Vault secret | Required value |
| --- | --- |
| `Mcp--External--PublicBaseUrl` | `https://20.235.26.238.sslip.io` |
| `Mcp--External--Audience` | `https://20.235.26.238.sslip.io/mcp` |
| `Mcp--External--Issuer` | Your Auth0 tenant issuer, including its trailing slash |

`Mcp--External--Audience` must not retain the previous origin-only API identifier. The backend
now rejects that mismatch at startup instead of appearing connected and failing later in Claude.

### Dynamic client registration and token claims

Enable Dynamic Client Registration in **Settings → Advanced**. Configure the MCP API's default
third-party **User-delegated Access** to allow only `speedclaim.catalog.read` and
`speedclaim.account.read`. Promote the desired Username-Password and social connections to domain
level so dynamically registered clients can use them.

Create and deploy an Auth0 **Post Login** Action that runs only for the SpeedClaim MCP API
audience and adds these claims to the *access token*:

```js
exports.onExecutePostLogin = async (event, api) => {
  if (event.resource_server?.identifier !== 'https://20.235.26.238.sslip.io/mcp') return;
  api.accessToken.setCustomClaim('https://20.235.26.238.sslip.io/claims/email', event.user.email);
  api.accessToken.setCustomClaim('https://20.235.26.238.sslip.io/claims/email_verified', event.user.email_verified === true);
};
```

Set `Mcp--External--AutoLinkVerifiedEmail` to `true` in Azure Key Vault only after this Action is
live, then restart `speedclaim-api`. The claim namespace must match the configured
`Mcp--External--PublicBaseUrl`; update both locations together if the public hostname changes.

### Manual fallback application

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

- Automatic linking accepts only a signed Auth0 verified-email claim and only links an exact match
  to one active, verified SpeedClaim customer. It does not accept email from an MCP argument,
  browser parameter, or AI host.
- The manual fallback links an Auth0 subject only to the already authenticated, active, verified
  SpeedClaim customer that initiated the browser transaction.
- A subject cannot be linked to a second customer, and a customer cannot be linked to a different
  Auth0 subject.
- The external MCP server exposes read-only tools only; it does not expose identity linking,
  payments, claim submissions, KYC uploads, applications, or status changes.
- The Auth0 client secret is used only by the API's server-side token exchange and is never sent to
  the Angular application or an AI host.
