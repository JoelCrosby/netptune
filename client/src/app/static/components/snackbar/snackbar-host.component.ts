import { Component, inject } from '@angular/core';
import { SnackbarComponent } from './snackbar.component';
import { SnackbarItem } from './snackbar.models';
import { SnackbarService } from './snackbar.service';

@Component({
  selector: 'app-snackbar-host',
  imports: [SnackbarComponent],
  template: `
    @if (service.items().length) {
      <div
        class="fixed inset-x-4 top-4 z-9999 flex flex-col items-stretch gap-2 md:inset-x-auto md:top-auto md:right-6 md:bottom-6 md:items-end"
        aria-live="polite"
        aria-atomic="false">
        @for (item of service.items(); track item.id) {
          <app-snackbar [item]="item" (actionClicked)="onAction($event)" />
        }
      </div>
    }
  `,
})
export class SnackbarHostComponent {
  readonly service = inject(SnackbarService);

  onAction(item: SnackbarItem): void {
    item.onAction?.();
    this.service.dismiss(item.id);
  }
}
