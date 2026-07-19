import { ApplicationConfig, inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors, withFetch } from '@angular/common/http';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';
import { routes } from './app.routes';
import { backendUrlInterceptor } from './core/interceptors/backend-url.interceptor';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import { AuthService } from './core/services/auth.service';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([backendUrlInterceptor, authInterceptor, errorInterceptor]), withFetch()),
    provideClientHydration(withEventReplay()),
    // Workspace is also available to guests, so it does not pass through an auth
    // guard. Restore an existing browser session before the first public route renders.
    provideAppInitializer(() => inject(AuthService).initFromStorage()),
  ],
};
