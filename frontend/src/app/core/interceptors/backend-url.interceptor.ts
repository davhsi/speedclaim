import { HttpInterceptorFn } from '@angular/common/http';
import { resolveBackendUrl } from '../config/backend-url.config';

export const backendUrlInterceptor: HttpInterceptorFn = (req, next) => {
  const resolvedUrl = resolveBackendUrl(req.url);
  return next(resolvedUrl === req.url ? req : req.clone({ url: resolvedUrl }));
};

