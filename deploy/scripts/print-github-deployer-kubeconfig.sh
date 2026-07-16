#!/usr/bin/env bash
set -euo pipefail

# Requires a current kubectl context with access to the SpeedClaim AKS cluster.
# It prints a namespace-scoped kubeconfig. Store the entire output as the
# AKS_DEPLOY_KUBECONFIG GitHub repository secret; never commit it.

namespace="speedclaim"
service_account="speedclaim-github-deployer"
token_secret="speedclaim-github-deployer-token"

server="$(kubectl config view --raw --minify -o jsonpath='{.clusters[0].cluster.server}')"
certificate_authority_data="$(kubectl config view --raw --minify -o jsonpath='{.clusters[0].cluster.certificate-authority-data}')"
encoded_token="$(kubectl -n "$namespace" get secret "$token_secret" -o jsonpath='{.data.token}')"
token="$(printf '%s' "$encoded_token" | base64 --decode 2>/dev/null || printf '%s' "$encoded_token" | base64 -D)"

if [ -z "$server" ] || [ -z "$certificate_authority_data" ] || [ -z "$token" ]; then
  echo "Could not create the GitHub deployer kubeconfig. Apply deploy/k8s/github-deployer.yaml first." >&2
  exit 1
fi

cat <<EOF
apiVersion: v1
kind: Config
clusters:
  - name: speedclaim-aks
    cluster:
      server: ${server}
      certificate-authority-data: ${certificate_authority_data}
users:
  - name: ${service_account}
    user:
      token: ${token}
contexts:
  - name: speedclaim-github
    context:
      cluster: speedclaim-aks
      namespace: ${namespace}
      user: ${service_account}
current-context: speedclaim-github
EOF
