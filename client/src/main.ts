import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { bootstrapApplication } from '@angular/platform-browser';
import {
  PreloadAllModules,
  provideRouter,
  withComponentInputBinding,
  withPreloading,
} from '@angular/router';
import { AppComponent } from './app/app.component';
import { routes } from './app/app.routes';
import { provideAuthRefresh } from './app/core/auth/auth.service';
import { authInterceptor } from './app/core/http-interceptors/auth.interceptor';
import { provideAppLoader } from './app/core/services/app-loader.service';
import { provideNavigationService } from './app/core/services/navigation.service';
import { provideVersionCheck } from './app/core/services/version-check.service';
import { provideWorkspaceBranding } from './app/core/services/workspace-branding.service';
import { provideNotificationEvents } from './app/core/sse/notification-sse.service';
import { provideWorkspaceEvents } from './app/core/sse/workspace-events.service';

bootstrapApplication(AppComponent, {
  providers: [
    provideRouter(
      routes,
      withComponentInputBinding(),
      withPreloading(PreloadAllModules)
    ),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAuthRefresh(),
    provideNavigationService(),
    provideAppLoader(),
    provideVersionCheck(),
    provideWorkspaceBranding(),
    provideWorkspaceEvents(),
    provideNotificationEvents(),
  ],
});
