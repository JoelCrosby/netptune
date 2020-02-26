import { ProjectsEffects } from './projects/projects.effects';
import { AuthInterceptor } from './http-interceptors/auth.interceptor';
import { CommonModule } from '@angular/common';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { NgModule, Optional, SkipSelf } from '@angular/core';
import { environment } from '@env/environment';
import { EffectsModule } from '@ngrx/effects';
import {
  RouterStateSerializer,
  StoreRouterConnectingModule,
} from '@ngrx/router-store';
import { StoreModule } from '@ngrx/store';
import { StoreDevtoolsModule } from '@ngrx/store-devtools';
import { AuthGuardService } from './auth/auth-guard.service';
import { AuthEffects } from './auth/store/auth.effects';
import { metaReducers, reducers } from './core.state';
import { CustomSerializer } from './router/custom-serializer';
import { SettingsEffects } from './settings/settings.effects';
import { CoreEffects } from './state/core.effects';
import { WorkspacesEffects } from './workspaces/workspaces.effects';

@NgModule({
  imports: [
    // angular
    CommonModule,
    HttpClientModule,

    // ngrx
    StoreModule.forRoot(reducers, {
      metaReducers,
      runtimeChecks: {
        strictStateImmutability: true,
        strictActionImmutability: true,
      },
    }),
    StoreRouterConnectingModule.forRoot(),
    EffectsModule.forRoot([
      AuthEffects,
      CoreEffects,
      SettingsEffects,
      WorkspacesEffects,
      ProjectsEffects,
    ]),
    environment.production
      ? []
      : StoreDevtoolsModule.instrument({
          name: 'Netptune',
        }),
  ],
  providers: [
    AuthGuardService,
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
    { provide: RouterStateSerializer, useClass: CustomSerializer },
  ],
})
export class CoreModule {
  constructor(
    @Optional()
    @SkipSelf()
    parentModule: CoreModule
  ) {
    if (parentModule) {
      throw new Error('CoreModule is already loaded. Import only in AppModule');
    }
  }
}
