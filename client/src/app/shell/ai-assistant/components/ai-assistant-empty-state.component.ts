import { Component, computed, inject, output } from '@angular/core';
import { selectCurrentUserDisplayName } from '@core/store/auth/auth.selectors';
import { Store } from '@ngrx/store';
import { LucideSparkles } from '@lucide/angular';

@Component({
  selector: 'app-ai-assistant-empty-state',
  host: {
    class:
      'flex flex-1 flex-col items-center justify-center gap-3 px-4 text-center',
  },
  imports: [LucideSparkles],
  template: `
    <span
      class="bg-hover text-muted flex h-12 w-12 items-center justify-center rounded-xl">
      <svg lucideSparkles class="h-5 w-5"></svg>
    </span>

    <h3 class="font-overpass text-lg font-medium">{{ greeting() }}</h3>

    <p
      class="text-muted max-w-sm text-sm"
      i18n="Empty state inside the AI assistant panel">
      Ask about your workspace — projects, tasks, statuses. Any change the
      assistant suggests is shown here for you to review before it is applied.
    </p>

    <div class="mt-2 flex max-w-sm flex-wrap justify-center gap-2">
      @for (prompt of prompts; track prompt) {
        <button
          type="button"
          class="bg-hover hover:bg-foreground/10 focus-visible:ring-primary rounded-full px-3 py-1.5 text-xs transition-colors focus-visible:ring-2 focus-visible:outline-none"
          (click)="selected.emit(prompt)">
          {{ prompt }}
        </button>
      }
    </div>
  `,
})
export class AiAssistantEmptyStateComponent {
  readonly selected = output<string>();

  /** First questions that show what the assistant can reach without teaching a syntax. */
  protected readonly prompts = [
    $localize`:Suggested first question in the assistant:What changed in this workspace today?`,
    $localize`:Suggested first question in the assistant:Summarise the current sprint`,
    $localize`:Suggested first question in the assistant:What should I work on next?`,
    $localize`:Suggested first question in the assistant:Find tasks with no assignee`,
  ];

  private readonly store = inject(Store);
  private readonly displayName = this.store.selectSignal(
    selectCurrentUserDisplayName
  );

  protected readonly greeting = computed(() => {
    const name = this.firstName();
    const hour = new Date().getHours();

    if (hour < 12) {
      return $localize`:Greeting shown in the assistant before noon:Morning, ${name}:name:!`;
    }

    if (hour < 18) {
      return $localize`:Greeting shown in the assistant during the afternoon:Afternoon, ${name}:name:!`;
    }

    return $localize`:Greeting shown in the assistant during the evening:Evening, ${name}:name:!`;
  });

  private firstName(): string {
    const name = this.displayName() ?? '';

    return name.split(' ')[0] || name;
  }
}
