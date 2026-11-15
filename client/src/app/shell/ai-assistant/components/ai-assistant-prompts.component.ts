import { Component, computed, input, output } from '@angular/core';

@Component({
  selector: 'app-ai-assistant-prompts',
  host: { class: 'block' },
  template: `
    <div class="flex flex-wrap justify-center" [class]="listClass()">
      @for (prompt of prompts; track prompt) {
        <button
          type="button"
          class="border-border text-muted hover:text-foreground hover:border-foreground/25 focus-visible:ring-primary rounded-full border transition-colors focus-visible:ring-2 focus-visible:outline-none"
          [class]="chipClass()"
          (click)="selected.emit(prompt)">
          {{ prompt }}
        </button>
      }
    </div>
  `,
})
export class AiAssistantPromptsComponent {
  readonly roomy = input(false);

  readonly selected = output<string>();

  /** First questions that show what the assistant can reach without teaching a syntax. */
  protected readonly prompts = [
    $localize`:Suggested first question in the assistant:What changed in this workspace today?`,
    $localize`:Suggested first question in the assistant:Summarise the current sprint`,
    $localize`:Suggested first question in the assistant:What should I work on next?`,
    $localize`:Suggested first question in the assistant:Find tasks with no assignee`,
  ];

  protected readonly listClass = computed(() => {
    return this.roomy() ? 'gap-2' : 'max-w-sm gap-2';
  });

  protected readonly chipClass = computed(() => {
    return this.roomy() ? 'px-3.5 py-2 text-[13px]' : 'px-3 py-1.5 text-xs';
  });
}
