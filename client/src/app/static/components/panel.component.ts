import { Component } from '@angular/core';

@Component({
  selector: 'app-panel',
  imports: [],
  host: {
    class:
      'block overflow-hidden rounded-lg border border-border bg-background shadow-sm',
  },
  template: `<ng-content />`,
  styles: ``,
})
export class PanelComponent {}
