import { Component, input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  imports: [],
  host: { class: 'block' },
  template: `<div
    class="my-10 flex h-full flex-col items-center justify-center gap-2 text-center">
    <div class="text-muted-foreground" aria-hidden="true">
      <ng-content select="[emptyStateIcon]" />
    </div>

    <h4 class="mx-8 font-normal">{{ title() }}</h4>

    @if (description()) {
      <p class="text-foreground/70 mb-2 text-sm">{{ description() }}</p>
    }

    <div class="mt-2 empty:hidden">
      <ng-content select="[emptyStateAction]" />
    </div>
  </div>`,
})
export class EmptyStateComponent {
  readonly title = input.required<string>();
  readonly description = input('');
}
