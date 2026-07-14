import { describe, expect, it } from 'vitest';
import { resolveBackendUrl } from './backend-url.config';

describe('resolveBackendUrl', () => {
  it('keeps backend URLs relative when no backend origin is configured', () => {
    expect(resolveBackendUrl('/api/v1/users', '')).toBe('/api/v1/users');
    expect(resolveBackendUrl('/uploads/avatar.png', '')).toBe('/uploads/avatar.png');
    expect(resolveBackendUrl('/hubs/notifications', '')).toBe('/hubs/notifications');
  });

  it('prefixes backend URLs with the configured build-environment origin', () => {
    expect(resolveBackendUrl('/api/v1/users', 'https://api.example.test/'))
      .toBe('https://api.example.test/api/v1/users');
  });

  it('does not rewrite absolute or frontend URLs', () => {
    expect(resolveBackendUrl('https://other.example.test/api/v1/users', 'https://api.example.test'))
      .toBe('https://other.example.test/api/v1/users');
    expect(resolveBackendUrl('/claims', 'https://api.example.test')).toBe('/claims');
  });
});
