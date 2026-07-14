import { environment } from '../../../environments/environment';

const BACKEND_PREFIXES = ['/api/', '/uploads/', '/hubs/'];

function isAbsoluteUrl(url: string): boolean {
  return /^https?:\/\//i.test(url);
}

export function resolveBackendUrl(url: string, backendOrigin: string = environment.backendOrigin): string {
  if (isAbsoluteUrl(url) || !BACKEND_PREFIXES.some(prefix => url.startsWith(prefix))) {
    return url;
  }

  const normalizedOrigin = backendOrigin.trim().replace(/\/$/, '');
  return normalizedOrigin ? `${normalizedOrigin}${url}` : url;
}
