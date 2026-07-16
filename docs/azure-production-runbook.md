# SpeedClaim production deployment runbook

## Migration rule

Keep `rg-davish` and its existing Static Web Apps workflow alive until this replacement passes
the smoke tests below. The old workflow uses the old Static Web App deployment token and cannot
deploy to the new resource group.

## 1. Deploy infrastructure

From the repository root, follow the preview and deployment commands in `infra/README.md`.
Use the output values to record the new ACR, AKS, Key Vault, PostgreSQL, Service Bus, Function
App, and Static Web App names.

## 2. Populate Key Vault

Set the following secrets manually. Azure Key Vault translates `--` into .NET configuration
section separators when the API loads the vault.

- `ConnectionStrings--DefaultConnection`
- `JwtSettings--Secret`
- `SecuritySettings--EncryptionKey`
- `Stripe--SecretKey`
- `Stripe--PublishableKey`
- `Stripe--WebhookSecret`
- `AzureBlob--ConnectionString`
- `AzureBlob--ContainerName`
- `SmtpSettings--Host`
- `SmtpSettings--Port`
- `SmtpSettings--SenderName`
- `SmtpSettings--SenderEmail`
- `SmtpSettings--AppPassword`
- `AI--InternalApiKey`
- `AI--VectorConnectionString`
- `AI--AnthropicBaseUrl`
- `AI--AnthropicAuthToken`
- `EmailDelivery--ServiceBusConnectionString`

The AKS API uses its workload identity to read these values. The AI pod receives only the
specific values it needs through its Kubernetes secret bootstrap; it never receives the business
database connection string.

The deployment has already seeded the database, Blob, Service Bus, internal API, JWT, and
encryption values. You still need to add the real Stripe, SMTP, and Anthropic gateway values.
For Anthropic, use a merge patch so the existing generated AI secret entries are retained:

```bash
kubectl -n speedclaim patch secret speedclaim-ai-secrets --type merge -p \
  "{\"stringData\":{\"ANTHROPIC_BASE_URL\":\"https://your-gateway\",\"ANTHROPIC_AUTH_TOKEN\":\"your-token\"}}"
kubectl -n speedclaim rollout restart deployment/speedclaim-ai
```

The AI settings also accept the equivalent `AI__AnthropicBaseUrl` and
`AI__AnthropicAuthToken` names.

## 3. Database setup

Enable `vector` in the `speedclaim_ai` database, then run Alembic against that database. Apply
the .NET EF Core migrations against `speedclaim`. This separation prevents the AI service from
writing business records.

## 4. Service Bus email setup

Retrieve the `api-email-sender` SAS connection string and set it in Key Vault as
`EmailDelivery--ServiceBusConnectionString`. The API deployment sets
`EmailDelivery__Provider=ServiceBus` and `EmailDelivery__QueueName=email-dispatch`; Key Vault
then supplies the connection string. The Function App already has the listener-only policy from
Bicep and sends queued, non-attachment email through SMTP.

## 5. AKS bootstrap

1. Use `az aks get-credentials` to configure `kubectl` for the new cluster.
2. Create `speedclaim-ai-secrets` from a copy of
   `deploy/k8s/ai-secret.example.yaml` stored outside the repository. Its connection string must
   target the `speedclaim_ai` database with `sslmode=require`.
3. Run `deploy/scripts/install-ingress.sh <your-email>`. It installs ingress-nginx and
   cert-manager, then prints a temporary `https://<public-ip>.sslip.io` API hostname. This is a
   practical demo endpoint, not a permanent production domain.
4. Build and push both containers, then set the variables listed at the top of
   `deploy/scripts/apply-workloads.sh` and run it. Use the hostname printed in the preceding
   step as `API_HOST`.
5. Build the Angular production configuration with that exact HTTPS origin configured as its
   backend origin before deploying the Static Web App.

The API service account uses the Bicep-created workload identity to read Key Vault. The
registry pull secret is deliberate temporary Contributor-compatible plumbing: replace it with
an AKS kubelet `AcrPull` assignment when an Owner or User Access Administrator is available.
When building from an Apple Silicon workstation, publish Linux AMD64 images explicitly with
`docker buildx build --platform linux/amd64 --push`; AKS Linux node pools do not run ARM64
images by default.

## 6. GitHub Actions secrets and migration

1. Retrieve the deployment token for the **new** Static Web App. Do not copy the old
   `GREEN_BUSH` token; it can deploy only to the old site.

   ```bash
   az staticwebapp secrets list --resource-group rg-speedclaim-prod \
     --name swa-speedclaim-acvq6nlrs3xns --query properties.apiKey --output tsv
   ```

2. Create the namespace-scoped AKS deployment credential from an operator account that can
   already use `kubectl` against `aks-speedclaim-prod`:

   ```bash
   kubectl apply -f deploy/k8s/github-deployer.yaml
   ./deploy/scripts/print-github-deployer-kubeconfig.sh
   ```

   Copy the complete YAML printed by the second command into the
   `AKS_DEPLOY_KUBECONFIG` GitHub repository secret. It can modify only `speedclaim`
   namespace workloads and supporting secrets, not the AKS cluster.

3. Create these repository secrets. Do not copy the old `GREEN_BUSH` token: it belongs only to
   the old Static Web App.

| Secret | Used by |
| --- | --- |
| `AZURE_STATIC_WEB_APPS_API_TOKEN_SPEEDCLAIM_PROD` | new frontend workflow |
| `ACR_LOGIN_SERVER`, `ACR_USERNAME`, `ACR_PASSWORD` | container build/push and AKS image pull |
| `AKS_DEPLOY_KUBECONFIG` | container workflow; a namespace-scoped service account credential |
| `API_WORKLOAD_IDENTITY_CLIENT_ID`, `KEY_VAULT_URI`, `API_PUBLIC_HOST`, `FRONTEND_ORIGIN` | rendered AKS workloads (`FRONTEND_ORIGIN` is the new Static Web App HTTPS URL) |
| `API_PUBLIC_ORIGIN` | frontend build (`https://<public-ip>.sslip.io`) |
| `AZURE_FUNCTIONAPP_NAME_SPEEDCLAIM_PROD`, `AZURE_FUNCTIONAPP_PUBLISH_PROFILE_SPEEDCLAIM_PROD` | Function deployment |

4. Push a frontend-only change and confirm the new Static Web App deploys. The old workflow is
   intentionally left in place during this migration, so both sites receive the deployment.
5. After the new endpoint and frontend pass the smoke tests, retire the old workflow and only
   then delete the old Static Web App or `rg-davish`.

## Smoke tests

- Static Web App loads over HTTPS.
- API health endpoint responds over HTTPS.
- Login and an upload to Blob Storage work.
- Brochure ingestion and a grounded policy question work through the private AI service.
- A non-attachment transactional email enters `email-dispatch` and is delivered by the Function.
- A deliberately bad email message reaches the Service Bus dead-letter queue after retries.
