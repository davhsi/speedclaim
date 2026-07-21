# SpeedClaim Azure deployment status and operating plan

> Current as of 2026-07-21. This replaces the earlier pre-deployment checklist. Treat live Azure
> state as something to verify before each operation; this document contains no credentials.

## Current environment

| Area | Current value |
| --- | --- |
| Subscription / resource group | `Training-2026` / `rg-davish` |
| Frontend | Azure Static Web Apps: `https://green-bush-029304c00.7.azurestaticapps.net` |
| Public API | `https://speedclaim-api-davish.southindia.cloudapp.azure.com` |
| Container registry | `acrspeedclaim.azurecr.io` |
| AKS | `aks-speedclaim-davish` |
| PostgreSQL Flexible Server | `speedclaim.postgres.database.azure.com` |
| Blob Storage | `stspeedclaimdavish`, container `speedclaim-uploads` |
| Key Vault | `kv-speedclaim-davish` |

The local Kubernetes manifest currently references `acrspeedclaim.azurecr.io/speedclaim-api:v5`.
Never report that as the live image without checking the running deployment first.

## Configuration and secrets

- The production API opts into Key Vault only when `KeyVault:Uri` is non-empty. Local
  development must leave it absent/empty, use `Storage:Provider=Local`, and use a localhost
  PostgreSQL connection.
- Keep secrets in Key Vault or the deployment environment. Do not commit populated Kubernetes
  `Secret` manifests, production connection strings, or access tokens.
- Non-sensitive production configuration belongs in ConfigMaps/build configuration. Backend
  origins are not frontend secrets.
- The AI service stays private behind the .NET API. The external MCP connector is disabled and
  must not be exposed publicly until the OAuth/OIDC and security work described in
  [mcp-architecture.md](mcp-architecture.md) is complete.

## Known deployment constraint

The AKS-to-ACR attach workflow could not assign `AcrPull` because the current user has
Contributor access without `Microsoft.Authorization/roleAssignments/write`. An ACR image-pull
secret is the temporary demo workaround. For a durable setup, an Owner or User Access
Administrator must grant `AcrPull` to the AKS kubelet identity.

## Routine release checks

1. Build and test the changed layers locally.
2. Confirm the frontend’s production backend origin and API CORS configuration agree.
3. Verify the active AKS image, pods, API health, and required Key Vault/configuration values.
4. Verify upload storage and a representative authenticated portal flow.
5. Roll back to the previously verified image/configuration if the smoke test fails; do not
   alter databases or secrets as a first rollback step.

Azure Static Web Apps builds pull requests targeting `main` and deploys production frontend code
on pushes to `main`. Use a reviewed pull request and merge rather than direct changes to `main`.

## Useful local commands

```bash
dotnet build backend/SpeedClaim.Api
dotnet test backend/SpeedClaim.Tests
cd frontend && npm run build -- --configuration production
cd ai-service && .venv/bin/python -m pytest
```

For the clean-slate Bicep experiment, see [infra/README.md](../infra/README.md). It is a proposed
environment plan, not a substitute for the live state above.
