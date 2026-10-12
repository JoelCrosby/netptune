import { Component, input } from '@angular/core';

@Component({
  selector: 'app-header-prompt',
  host: { class: 'flex items-center gap-3.5' },
  template: `
    @if (prompt(); as prompt) {
      <span
        class="text-foreground/55 hidden text-[13px] whitespace-nowrap sm:inline">
        {{ prompt }}
      </span>
    }

    <ng-content />
  `,
})
export class HeaderPromptComponent {
  readonly prompt = input<string | null>(null);
}
