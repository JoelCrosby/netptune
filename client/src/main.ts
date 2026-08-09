import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { bootstrapApplication } from '@angular/platform-browser';
import {
  PreloadAllModules,
  provideRouter,
  withComponentInputBinding,
  withPreloading,
} from '@angular/router';
import { provideEffects } from '@ngrx/effects';
import { provideStore } from '@ngrx/store';
import { AppComponent } from './app/app.component';
import { routes } from './app/app.routes';
import { provideAuthRefresh } from './app/core/auth/auth.service';
import { metaReducers, reducers } from './app/core/core.state';
import { authInterceptor } from './app/core/http-interceptors/auth.interceptor';
import { provideNavigationService } from './app/core/services/navigation.service';
import { provideVersionCheck } from './app/core/services/version-check.service';
import { provideNotificationEvents } from './app/core/sse/notification-sse.service';
import { provideWorkspaceEvents } from './app/core/sse/workspace-events.service';
import { WorkspacesEffects } from './app/core/store/workspaces/workspaces.effects';

bootstrapApplication(AppComponent, {
  providers: [
    provideRouter(
      routes,
      withComponentInputBinding(),
      withPreloading(PreloadAllModules)
    ),
    provideStore(reducers, {
      metaReducers,
      runtimeChecks: {
        strictStateImmutability: true,
        strictActionImmutability: true,
        strictStateSerializability: false,
        strictActionSerializability: false,
        strictActionTypeUniqueness: true,
      },
    }),
    provideEffects([WorkspacesEffects]),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAuthRefresh(),
    provideNavigationService(),
    provideVersionCheck(),
    provideWorkspaceEvents(),
    provideNotificationEvents(),
  ],
});
