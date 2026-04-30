import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { SnackbarComponent } from './snackbar.component';
import { SnackbarService } from './snackbar.service';

@Component({
  selector: 'app-snackbar-host',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [SnackbarComponent],
  template: `
    @if (service.items().length) {
      <div
        class="fixed bottom-6 left-1/2 z-9999 flex -translate-x-1/2 flex-col items-center gap-2"
        aria-live="polite"
        aria-atomic="false">
        @for (item of service.items(); track item.id) {
          <app-snackbar [item]="item" />
        }
      </div>
    }
  `,
})
export class SnackbarHostComponent {
  readonly service = inject(SnackbarService);
}
