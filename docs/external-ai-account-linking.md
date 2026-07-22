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

### Dynamic client registration and token claims

Enable Dynamic Client Registration in **Settings → Advanced**. Configure the MCP API's default
third-party **User-delegated Access** to allow only `speedclaim.catalog.read` and
`speedclaim.account.read`. Promote the desired Username-Password and social connections to domain
level so dynamically registered clients can use them.

Create and deploy an Auth0 **Post Login** Action that runs only for the SpeedClaim MCP API
audience and adds these claims to the *access token*:

```js
exports.onExecutePostLogin = async (event, api) => {
  if (event.resource_server?.identifier !== 'https://20.235.26.238.sslip.io/') return;
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
