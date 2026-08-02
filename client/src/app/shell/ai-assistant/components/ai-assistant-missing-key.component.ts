import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideTriangleAlert } from '@lucide/angular';

@Component({
  selector: 'app-ai-assistant-missing-key',
  host: { class: 'block' },
  imports: [LucideTriangleAlert, RouterLink],
  template: `
    <div
      class="bg-card-warn border-border flex items-start gap-2 rounded-lg border px-3 py-2.5 text-sm"
      role="status">
      <svg lucideTriangleAlert class="text-warn mt-0.5 h-4 w-4 shrink-0"></svg>

      <div class="min-w-0">
        <p i18n="Shown in the assistant when the user has no provider API key">
          The assistant needs your own provider API key before it can answer.
        </p>

        @if (settingsLink(); as link) {
          <a
            [routerLink]="link"
            class="text-primary font-medium hover:underline"
            i18n="Link to the settings page where an API key is added">
            Add a key in personal settings
          </a>
        }
      </div>
    </div>
  `,
})
export class AiAssistantMissingKeyComponent {
  readonly workspace = input<string | null>(null);

  protected readonly settingsLink = computed(() => {
    const workspace = this.workspace();

    if (!workspace) {
      return null;
    }

    return ['/', workspace, 'settings', 'personal', 'assistant'];
  });
}
