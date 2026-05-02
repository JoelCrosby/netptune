import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { inject, provideAppInitializer } from '@angular/core';
import { bootstrapApplication } from '@angular/platform-browser';
import { provideRouter, withViewTransitions } from '@angular/router';
import { provideEffects } from '@ngrx/effects';
import { provideRouterStore } from '@ngrx/router-store';
import { provideStore, Store } from '@ngrx/store';
import { catchError, firstValueFrom, of, switchMap, tap } from 'rxjs';
import { AppComponent } from './app/app.component';
import { routes } from './app/app.routes';
import { AuthService } from './app/core/auth/auth.service';
import { refreshTokenSuccess } from './app/core/store/auth/auth.actions';
import { AuthEffects } from './app/core/store/auth/auth.effects';
import { selectShouldRefreshSession } from './app/core/store/auth/auth.selectors';
import { metaReducers, reducers } from './app/core/core.state';
import { authInterceptor } from './app/core/http-interceptors/auth.interceptor';
import { CustomSerializer } from './app/core/router/custom-serializer';
import { NavigationService } from './app/core/services/navigation.service';
import { LayoutEffects } from './app/core/store/layout/layout.effects';
import { MetaEffects } from './app/core/store/meta/meta.effects';
import { SettingsEffects } from './app/core/store/settings/settings.effects';
import { WorkspacesEffects } from './app/core/store/workspaces/workspaces.effects';

bootstrapApplication(AppComponent, {
  providers: [
    provideRouter(routes, withViewTransitions()),
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
    provideRouterStore({
      serializer: CustomSerializer,
    }),
    provideEffects([
      AuthEffects,
      MetaEffects,
      LayoutEffects,
      SettingsEffects,
      WorkspacesEffects,
    ]),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAppInitializer(() => {
      const authService = inject(AuthService);
      const store = inject(Store);

      return firstValueFrom(
        store.select(selectShouldRefreshSession).pipe(
          switchMap((shouldRefresh) => {
            if (!shouldRefresh) return of(null);

            return authService.refresh().pipe(
              tap((user) => store.dispatch(refreshTokenSuccess({ user }))),
              catchError(() => of(null))
            );
          })
        )
      );
    }),
    provideAppInitializer(() => {
      inject(NavigationService).listen();
    }),
  ],
});
