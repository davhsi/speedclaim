#!/usr/bin/env bash
set -euo pipefail

# Demo endpoint only: sslip.io gives the load-balancer IP a DNS name so Let's Encrypt can
# complete HTTP-01 validation without a custom domain. Replace it with a real domain later.
LETSENCRYPT_EMAIL=${1:?Usage: install-ingress.sh <lets-encrypt-email>}

helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx
helm repo add jetstack https://charts.jetstack.io
helm repo update
helm upgrade --install ingress-nginx ingress-nginx/ingress-nginx \
  --namespace ingress-nginx --create-namespace \
  --set controller.service.externalTrafficPolicy=Local
helm upgrade --install cert-manager jetstack/cert-manager \
  --namespace cert-manager --create-namespace \
  --set crds.enabled=true

kubectl -n ingress-nginx rollout status deployment/ingress-nginx-controller --timeout=10m
IP=$(kubectl -n ingress-nginx get service ingress-nginx-controller \
  -o jsonpath='{.status.loadBalancer.ingress[0].ip}')
test -n "$IP"
API_HOST="${IP}.sslip.io"

sed "s|__LETSENCRYPT_EMAIL__|${LETSENCRYPT_EMAIL}|g" deploy/k8s/cluster-issuers.yaml | kubectl apply -f -
printf 'Ingress public host: https://%s\n' "$API_HOST"
