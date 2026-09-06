import { Component, input } from '@angular/core';

@Component({
  selector: 'app-assistant-section-heading',
  host: { class: 'flex items-center gap-3' },
  template: `
    <span class="text-muted text-[11px] font-bold tracking-[0.14em] uppercase">
      {{ label() }}
    </span>
    <span class="bg-foreground/10 h-px flex-1"></span>
  `,
})
export class AssistantSectionHeadingComponent {
  readonly label = input.required<string>();
}
