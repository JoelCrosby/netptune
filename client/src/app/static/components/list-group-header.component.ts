import { Component, input } from '@angular/core';

@Component({
  selector: 'app-list-group-header',
  host: {
    class:
      'bg-card-header border-border text-muted sticky top-0 z-10 flex items-center gap-2 border-b px-4 py-2 text-xs font-semibold tracking-wide uppercase',
  },
  template: `
    {{ label() }}
    @if (count() !== null) {
      <span class="text-muted/70">·&nbsp;{{ count() }}</span>
    }
  `,
})
export class ListGroupHeaderComponent {
  readonly label = input.required<string>();
  readonly count = input<number | null>(null);
}
