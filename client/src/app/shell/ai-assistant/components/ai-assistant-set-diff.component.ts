import { Component, computed, input } from '@angular/core';
import {
  AiChangeField,
  AiChangeValue,
  AiChangeValueKind,
} from '@core/models/ai-conversation';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import {
  ValueChipComponent,
  ValueChipTone,
} from '@static/components/value-chip/value-chip.component';
import { valueDiff } from './ai-assistant-change-kind';

interface AiSetEntry {
  key: string;
  value: AiChangeValue;
  tone: ValueChipTone;
}

const toEntries = (
  values: AiChangeValue[],
  tone: ValueChipTone
): AiSetEntry[] => {
  return values.map((value) => {
    return { key: `${tone}:${value.id ?? value.display}`, value, tone };
  });
};

@Component({
  selector: 'app-ai-assistant-set-diff',
  host: { class: 'flex flex-wrap items-center gap-2 px-4 py-3.5' },
  imports: [AvatarComponent, ValueChipComponent],
  template: `
    @for (entry of entries(); track entry.key) {
      <app-value-chip
        [label]="entry.value.display"
        [color]="entry.value.color ?? null"
        [tone]="entry.tone"
        [class]="isUser() ? 'py-0.5 pl-0.5' : ''">
        @if (isUser()) {
          <app-avatar
            size="xs"
            [tooltip]="false"
            [name]="entry.value.display"
            [imageUrl]="entry.value.pictureUrl" />
        }
      </app-value-chip>
    } @empty {
      <app-value-chip
        tone="empty"
        i18n-label="Shown when a change leaves a list of values empty"
        label="None" />
    }
  `,
})
export class AiAssistantSetDiffComponent {
  readonly field = input.required<AiChangeField>();

  protected readonly isUser = computed(() => {
    return this.field().kind === AiChangeValueKind.user;
  });

  protected readonly entries = computed(() => {
    const diff = valueDiff(this.field());

    return [
      ...toEntries(diff.removed, 'removed'),
      ...toEntries(diff.added, 'added'),
      ...toEntries(diff.kept, 'kept'),
    ];
  });
}
