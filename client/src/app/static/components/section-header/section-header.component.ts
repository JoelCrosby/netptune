import { Component, input } from '@angular/core';

@Component({
  selector: 'app-section-header',
  host: { class: 'block' },
  template: `<header class="mb-4 flex items-start justify-between gap-3">
    <div class="min-w-0">
      <h3 class="font-overpass text-[1.4rem] font-normal">{{ heading() }}</h3>

      @if (description()) {
        <p class="text-muted mt-1 text-sm">{{ description() }}</p>
      }
    </div>

    <div class="shrink-0 empty:hidden">
      <ng-content select="[sectionHeaderActions]" />
    </div>
  </header>`,
})
export class SectionHeaderComponent {
  readonly heading = input.required<string>();
  readonly description = input('');
}
