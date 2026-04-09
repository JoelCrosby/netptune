import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SnackbarHostComponent } from './static/components/snackbar/snackbar-host.component';

@Component({
  selector: 'app-root',
  template: `
    <div class="app-container">
      <router-outlet />
      <app-snackbar-host />
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, SnackbarHostComponent],
})
export class AppComponent {}
