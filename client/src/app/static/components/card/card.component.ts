import { Component } from '@angular/core';

@Component({
  selector: 'app-card',
  template: `
    <div
      class="border-border bg-card-header flex h-full min-h-24 flex-col rounded border shadow-sm">
      <div class="flex flex-col overflow-hidden px-6 py-4">
        <ng-content select="app-card-header-image" />
        <ng-content select="app-card-header" />
      </div>

      <div
        class="bg-card border-border flex flex-1 flex-col rounded border-t p-6">
        <ng-content select="app-card-content" />
        <ng-content />
      </div>
    </div>
  `,
})
export class CardComponent {}
