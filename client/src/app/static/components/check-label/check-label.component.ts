import { Component } from '@angular/core';
import { LucideCheck } from '@lucide/angular';

@Component({
  selector: 'app-check-label',
  imports: [LucideCheck],
  host: {
    class: 'text-primary inline-flex items-center gap-1.5 text-sm font-medium',
  },
  template: `
    <svg lucideCheck class="h-4 w-4" aria-hidden="true"></svg>
    <ng-content />
  `,
})
export class CheckLabelComponent {}
