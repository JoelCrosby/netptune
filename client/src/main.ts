import { enableProdMode, NgModuleRef, ApplicationRef } from '@angular/core';
import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';

import { AppModule } from './app/app.module';
import { environment } from '@env/environment';
import { enableDebugTools } from '@angular/platform-browser';

if (environment.production) {
  enableProdMode();
}

// Using Profile
// Execute the following in the devtools console:
// > ng.profiler.timeChangeDetection()

const addProfiling = (module: NgModuleRef<AppModule>): void => {
  if (environment.production) return;

  enableDebugTools(module.injector.get(ApplicationRef).components[0]);
};

platformBrowserDynamic()
  .bootstrapModule(AppModule)
  .then((module) => addProfiling(module))
  .catch((err) => console.log(err));
