# SpeedClaim Azure infrastructure

This folder is the repeatable, clean-slate Azure deployment for SpeedClaim. It targets one
production resource group: `rg-speedclaim-prod` in South India.

## What Bicep creates

- Azure Kubernetes Service for the public .NET API and private Python RAG service.
- Azure Container Registry with the administrator account enabled as the temporary
  Contributor-only image-pull and GitHub CI workaround.
- Azure Database for PostgreSQL Flexible Server 17 with separate `speedclaim` and
  `speedclaim_ai` databases.
- Private Blob containers for uploads (`speedclaim-uploads`) and API logs (`speedclaim-logs`).
  The log lifecycle policy moves API logs to Cool after 30 days and Archive after 180 days;
  it does not delete them.
- Key Vault using access policies, rather than Azure RBAC, so the deployment can work with
  Contributor access.
- Standard Service Bus namespace and an `email-dispatch` queue.
- A .NET 10 isolated Function App on Flex Consumption that consumes the email queue.
- Log Analytics with 180-day retention and Application Insights for the Function App.
- A new Azure Static Web App. GitHub deploys frontend code after its deployment token is added
  as a repository secret.

## Intentional Contributor-only trade-offs

The template does **not** create Azure role assignments. Contributor cannot create them.

- AKS pulls images through an ACR image-pull secret.
- The API and AI services receive secrets through Kubernetes bootstrap secrets generated from
  Key Vault values during deployment.
- Service Bus uses separate SAS policies: one send-only policy for the API and one listen-only
  policy for the Function App.
- The Function App accesses Key Vault with an access policy assigned to its user-assigned
  managed identity.
- Function host and deployment storage use connection strings because this Contributor-only
  subscription cannot create the Storage Blob Data Contributor role assignment required for
  managed-identity storage access.

## Before deploying

1. Sign in with `az login` and select `Training-2026`.
2. Get your Entra object ID:

   ```bash
   az ad signed-in-user show --query id --output tsv
   ```

3. Run a preview. The password is supplied interactively and is never stored in this repository:

   ```bash
   az deployment sub what-if \
     --location southindia \
     --template-file infra/main.bicep \
     --parameters infra/parameters/prod.bicepparam \
     deployerObjectId='<your-object-id>' \
     postgresAdministratorPassword='<new-strong-password>'
   ```

4. Create the deployment only after reviewing the preview:

   ```bash
   az deployment sub create \
     --location southindia \
     --template-file infra/main.bicep \
     --parameters infra/parameters/prod.bicepparam \
     deployerObjectId='<your-object-id>' \
     postgresAdministratorPassword='<new-strong-password>'
   ```

## Required bootstrap after Bicep

Bicep never receives or commits application secrets. Populate Key Vault with the existing
application secrets plus the new values listed in [the production runbook](../docs/azure-production-runbook.md).

Enable the PostgreSQL `vector` extension in the `speedclaim_ai` database, then run the Alembic
migration. Bicep can create the database but cannot safely execute SQL inside it.

Do not delete `rg-davish` until all smoke tests against `rg-speedclaim-prod` pass.
