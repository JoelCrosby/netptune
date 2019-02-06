import { NgModule, Optional, SkipSelf } from '@angular/core';
import { AppLoadModule } from './app-load/app-load.module';
import { LocalStorageService } from './local-storage/local-storage.service';

@NgModule({
  imports: [
    AppLoadModule
  ],
  declarations: [],
  providers: [
    LocalStorageService,
  ]
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
