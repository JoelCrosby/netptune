import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SnackbarHostComponent } from './static/components/snackbar/snackbar-host.component';

@Component({
  selector: 'app-root',
  template: `
    <div class="bg-background">
      <router-outlet />
      <app-snackbar-host />
    </div>
  `,
  imports: [RouterOutlet, SnackbarHostComponent],
})
export class AppComponent {}
