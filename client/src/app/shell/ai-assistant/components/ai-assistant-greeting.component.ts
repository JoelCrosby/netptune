import { Component, computed, inject, input } from '@angular/core';
import { SessionService } from '@core/services/session.service';
import { LucideSparkles } from '@lucide/angular';

@Component({
  selector: 'app-ai-assistant-greeting',
  host: { class: 'block' },
  imports: [LucideSparkles],
  template: `
    <div class="flex items-center justify-center" [class]="rowClass()">
      <svg
        lucideSparkles
        class="text-primary shrink-0"
        [class]="iconClass()"></svg>
      <h3 class="font-overpass font-medium" [class]="titleClass()">
        {{ greeting() }}
      </h3>
    </div>
  `,
})
export class AiAssistantGreetingComponent {
  readonly roomy = input(false);

  private readonly displayName = inject(SessionService).displayName;

  protected readonly rowClass = computed(() => {
    return this.roomy() ? 'gap-3' : 'gap-2';
  });

  protected readonly iconClass = computed(() => {
    return this.roomy() ? 'h-7 w-7' : 'h-5 w-5';
  });

  protected readonly titleClass = computed(() => {
    return this.roomy() ? 'text-[28px] leading-tight' : 'text-lg';
  });

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
