import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SnackbarHostComponent } from './static/components/snackbar/snackbar-host.component';
import { BannerComponent } from './static/components/banner/banner.component';

@Component({
  selector: 'app-root',
  template: `
    <div class="bg-background">
      <app-banner />
      <router-outlet />
      <app-snackbar-host />
    </div>
  `,
  imports: [RouterOutlet, SnackbarHostComponent, BannerComponent],
})
export class AppComponent {}
