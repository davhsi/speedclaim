const PRODUCTION_BACKEND_ORIGIN = 'https://speedclaim-api-davish.southindia.cloudapp.azure.com';

const BACKEND_PREFIXES = ['/api/', '/uploads/', '/hubs/'];

function isBrowser(): boolean {
  return typeof window !== 'undefined' && typeof window.location !== 'undefined';
}

function isAbsoluteUrl(url: string): boolean {
  return /^https?:\/\//i.test(url);
}

function shouldUseExternalBackend(): boolean {
  if (!isBrowser()) return false;

  const { hostname } = window.location;
  return hostname !== 'localhost' && hostname !== '127.0.0.1';
}

export function resolveBackendUrl(url: string): string {
  if (isAbsoluteUrl(url) || !BACKEND_PREFIXES.some(prefix => url.startsWith(prefix))) {
    return url;
  }

  return shouldUseExternalBackend() ? `${PRODUCTION_BACKEND_ORIGIN}${url}` : url;
}

