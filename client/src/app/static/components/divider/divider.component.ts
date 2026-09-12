import { Component, input } from '@angular/core';

@Component({
  selector: 'app-divider',
  host: { class: 'block', role: 'separator' },
  template: `
    @if (label(); as label) {
      <div class="flex items-center gap-3">
        <span class="bg-foreground/7 h-px grow"></span>
        <span
          class="text-foreground/45 text-[11px] font-bold tracking-[.1em] uppercase">
          {{ label }}
        </span>
        <span class="bg-foreground/7 h-px grow"></span>
      </div>
    } @else {
      <span class="bg-foreground/7 block h-px w-full"></span>
    }
  `,
})
export class DividerComponent {
  readonly label = input<string | null>(null);
}
