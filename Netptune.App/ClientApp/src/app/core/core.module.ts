import { NgModule, Optional, SkipSelf } from '@angular/core';
import { AuthGuardService } from './auth/auth-guard.service';
import { LocalStorageService } from './local-storage/local-storage.service';

@NgModule({
  imports: [],
  providers: [AuthGuardService, LocalStorageService],
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
