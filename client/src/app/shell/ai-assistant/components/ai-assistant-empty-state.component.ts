import { Component, output } from '@angular/core';
import { AiAssistantGreetingComponent } from './ai-assistant-greeting.component';
import { AiAssistantPromptsComponent } from './ai-assistant-prompts.component';

@Component({
  selector: 'app-ai-assistant-empty-state',
  host: {
    class:
      'flex flex-1 flex-col items-center justify-center gap-3 px-4 text-center',
  },
  imports: [AiAssistantGreetingComponent, AiAssistantPromptsComponent],
  template: `
    <app-ai-assistant-greeting />

    <p
      class="text-muted max-w-sm text-sm"
      i18n="Empty state inside the AI assistant panel">
      Ask about your workspace — projects, tasks, statuses. Any change the
      assistant suggests is shown here for you to review before it is applied.
    </p>

    <app-ai-assistant-prompts class="mt-2" (selected)="selected.emit($event)" />
  `,
})
export class AiAssistantEmptyStateComponent {
  readonly selected = output<string>();
}
