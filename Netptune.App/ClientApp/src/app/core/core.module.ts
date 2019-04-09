import { CommonModule } from '@angular/common';
import { NgModule, Optional, SkipSelf } from '@angular/core';
import { HttpClientModule } from '@angular/common/http';

import { AuthGuardService } from './auth/auth-guard.service';
import { LocalStorageService } from './local-storage/local-storage.service';

import { StoreModule } from '@ngrx/store';
import { StoreDevtoolsModule } from '@ngrx/store-devtools';
import { StoreRouterConnectingModule, RouterStateSerializer } from '@ngrx/router-store';
import { CustomSerializer } from './router/custom-serializer';

import { reducers, metaReducers } from './core.state';
import { EffectsModule } from '@ngrx/effects';
import { AuthEffects } from './auth/store/auth.effects';
import { environment } from '@env/environment';
import { httpInterceptorProviders } from './http-interceptors';

@NgModule({
  imports: [
    CommonModule,
    HttpClientModule,
    // ngrx
    StoreModule.forRoot(reducers, { metaReducers }),
    StoreRouterConnectingModule.forRoot(),
    EffectsModule.forRoot([AuthEffects]),
    environment.production
      ? []
      : StoreDevtoolsModule.instrument({
          name: 'Netptune',
        }),
  ],
  providers: [
    AuthGuardService,
    LocalStorageService,
    httpInterceptorProviders,
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
