#!/usr/bin/env bash
set -euo pipefail

# Required environment variables: ACR_LOGIN_SERVER, API_WORKLOAD_IDENTITY_CLIENT_ID,
# KEY_VAULT_URI, FRONTEND_ORIGIN, ACR_USERNAME, ACR_PASSWORD, API_IMAGE_TAG, AI_IMAGE_TAG, API_HOST.
: "${ACR_LOGIN_SERVER:?}"
: "${API_WORKLOAD_IDENTITY_CLIENT_ID:?}"
: "${KEY_VAULT_URI:?}"
: "${FRONTEND_ORIGIN:?}"
: "${ACR_USERNAME:?}"
: "${ACR_PASSWORD:?}"
: "${API_IMAGE_TAG:?}"
: "${AI_IMAGE_TAG:?}"
: "${API_HOST:?}"

kubectl -n speedclaim get serviceaccount speedclaim-github-deployer >/dev/null
kubectl -n speedclaim create secret docker-registry acr-pull \
  --docker-server="$ACR_LOGIN_SERVER" \
  --docker-username="$ACR_USERNAME" \
  --docker-password="$ACR_PASSWORD" \
  --dry-run=client -o yaml | kubectl apply -f -

render() {
  sed \
    -e "s|__ACR_LOGIN_SERVER__|${ACR_LOGIN_SERVER}|g" \
    -e "s|__API_WORKLOAD_IDENTITY_CLIENT_ID__|${API_WORKLOAD_IDENTITY_CLIENT_ID}|g" \
    -e "s|__KEY_VAULT_URI__|${KEY_VAULT_URI}|g" \
    -e "s|__FRONTEND_ORIGIN__|${FRONTEND_ORIGIN}|g" \
    -e "s|__API_IMAGE_TAG__|${API_IMAGE_TAG}|g" \
    -e "s|__AI_IMAGE_TAG__|${AI_IMAGE_TAG}|g" \
    -e "s|__API_HOST__|${API_HOST}|g" "$1"
}

kubectl -n speedclaim delete job/speedclaim-db-migrate --ignore-not-found
render deploy/k8s/db-migrate.yaml | kubectl apply -f -
kubectl -n speedclaim wait --for=condition=complete job/speedclaim-db-migrate --timeout=10m
render deploy/k8s/api.yaml | kubectl apply -f -
render deploy/k8s/ai.yaml | kubectl apply -f -
render deploy/k8s/ingress.yaml | kubectl apply -f -
kubectl -n speedclaim rollout status deployment/speedclaim-api --timeout=10m
kubectl -n speedclaim rollout status deployment/speedclaim-ai --timeout=10m
