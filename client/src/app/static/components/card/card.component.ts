import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-card',
  template: `
    <ng-content name="app-card-header-image" />
    <ng-content name="app-card-header" />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class:
      'shadow-sm flex flex-col p-6 rounded-sm border border-border bg-card min-h-24 overflow-hidden',
  },
})
export class CardComponent {}
