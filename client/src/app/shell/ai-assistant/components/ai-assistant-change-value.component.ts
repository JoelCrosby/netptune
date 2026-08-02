import { Component, computed, input } from '@angular/core';
import { AiChangeValue, AiChangeValueKind } from '@core/models/ai-conversation';
import { dateTimeFormat } from '@core/util/locale';
import { AvatarComponent } from '@static/components/avatar/avatar.component';

const dateFormat = dateTimeFormat({
  day: 'numeric',
  month: 'short',
  year: 'numeric',
});

@Component({
  selector: 'app-ai-assistant-change-value',
  host: { class: 'inline-flex min-w-0 items-center gap-1' },
  imports: [AvatarComponent],
  template: `
    @if (isUser()) {
      <app-avatar
        size="xs"
        [name]="value().display"
        [imageUrl]="value().pictureUrl ?? null"
        [tooltip]="false" />
    }

    @if (swatch(); as color) {
      <span
        class="h-2 w-2 shrink-0 rounded-full"
        [style.background-color]="color"></span>
    }

    <span class="min-w-0 truncate">{{ label() }}</span>
  `,
})
export class AiAssistantChangeValueComponent {
  readonly value = input.required<AiChangeValue>();
  readonly kind = input.required<AiChangeValueKind>();

  protected readonly isUser = computed(() => {
    return this.kind() === AiChangeValueKind.user;
  });

  protected readonly swatch = computed(() => {
    const isStatus = this.kind() === AiChangeValueKind.status;

    return isStatus ? (this.value().color ?? null) : null;
  });

  /** Dates arrive as ISO so they sort and compare; the reviewer should read theirs. */
  protected readonly label = computed(() => {
    const display = this.value().display;
    const isDate = this.kind() === AiChangeValueKind.date;

    if (!isDate) {
      return display;
    }

    const parsed = Date.parse(`${display}T00:00:00`);

    if (Number.isNaN(parsed)) {
      return display;
    }

    return dateFormat.format(parsed);
  });
}
