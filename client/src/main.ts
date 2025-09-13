import {
  ApplicationRef,
  enableProdMode,
  importProvidersFrom,
} from '@angular/core';
import {
  bootstrapApplication,
  BrowserModule,
  enableDebugTools,
} from '@angular/platform-browser';
import { environment } from '@env/environment';
import { SharedModule } from '@shared/shared.module';
import { StaticModule } from '@static/static.module';
import { AppComponent } from './app/app.component';
import { AppRoutingModule } from './app/app.routing.module';
import { CoreModule } from './app/core';

if (environment.production) {
  enableProdMode();
}

// Using Profile
// Execute the following in the devtools console:
// > ng.profiler.timeChangeDetection()

const addProfiling = (module: ApplicationRef): void => {
  if (environment.production) return;

  enableDebugTools(module.injector.get(ApplicationRef).components[0]);
};

bootstrapApplication(AppComponent, {
  providers: [
    importProvidersFrom(
      BrowserModule,
      // core & shared
      CoreModule,
      StaticModule,
      SharedModule,
      // app
      AppRoutingModule
    ),
  ],
})
  .then((module) => addProfiling(module))
  .catch((err) => console.log(err));
