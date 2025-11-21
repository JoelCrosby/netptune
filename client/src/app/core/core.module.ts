import { CommonModule } from '@angular/common';
import {
  HTTP_INTERCEPTORS,
  provideHttpClient,
  withInterceptorsFromDi,
} from '@angular/common/http';
import { NgModule, inject } from '@angular/core';
import { EntryModule } from '@entry/entry.module';
import { environment } from '@env/environment';
import { EffectsModule } from '@ngrx/effects';
import {
  RouterStateSerializer,
  StoreRouterConnectingModule,
} from '@ngrx/router-store';
import { StoreModule } from '@ngrx/store';
import { StoreDevtoolsModule } from '@ngrx/store-devtools';
import { CookieService } from 'ngx-cookie-service';
import { AuthEffects } from './auth/store/auth.effects';
import { metaReducers, reducers } from './core.state';
import { AuthInterceptor } from './http-interceptors/auth.interceptor';
import { CustomSerializer } from './router/custom-serializer';
import { ActivityEffects } from './store/activity/activity.effects';
import { LayoutEffects } from './store/layout/layout.effects';
import { MetaEffects } from './store/meta/meta.effects';
import { ProjectsEffects } from './store/projects/projects.effects';
import { SettingsEffects } from './store/settings/settings.effects';
import { TagsEffects } from './store/tags/tags.effects';
import { ProjectTasksEffects } from './store/tasks/tasks.effects';
import { UsersEffects } from './store/users/users.effects';
import { WorkspacesEffects } from './store/workspaces/workspaces.effects';

@NgModule({
  imports: [
    // angular
    CommonModule,
    // ngrx
    StoreModule.forRoot(reducers, {
      metaReducers,
      runtimeChecks: {
        strictStateImmutability: true,
        strictActionImmutability: true,
        strictStateSerializability: false,
        strictActionSerializability: false,
        strictActionTypeUniqueness: true,
      },
    }),
    StoreRouterConnectingModule.forRoot(),
    EffectsModule.forRoot([
      AuthEffects,
      MetaEffects,
      ActivityEffects,
      LayoutEffects,
      SettingsEffects,
      WorkspacesEffects,
      ProjectsEffects,
      ProjectTasksEffects,
      UsersEffects,
      TagsEffects,
    ]),
    environment.production
      ? []
      : StoreDevtoolsModule.instrument({
          name: 'Netptune',
          connectInZone: true,
        }),
    EntryModule,
  ],
  providers: [
    CookieService,
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
    { provide: RouterStateSerializer, useClass: CustomSerializer },
    provideHttpClient(withInterceptorsFromDi()),
  ],
})
export class CoreModule {
  constructor() {
    const parentModule = inject(CoreModule, { optional: true, skipSelf: true });

    if (parentModule) {
      throw new Error('CoreModule is already loaded. Import only in AppModule');
    }
  }
}
