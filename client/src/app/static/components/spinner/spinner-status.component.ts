import { Component } from '@angular/core';
import { SpinnerComponent } from './spinner.component';

@Component({
  selector: 'app-spinner-status',
  imports: [SpinnerComponent],
  host: {
    class: 'flex items-center gap-3.5 py-1',
    role: 'status',
  },
  template: `
    <app-spinner diameter="22px" />
    <span class="text-foreground/70 min-w-0 flex-1 text-sm">
      <ng-content />
    </span>
  `,
})
export class SpinnerStatusComponent {}
