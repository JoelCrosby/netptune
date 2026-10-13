import { Component, input } from '@angular/core';
import { AvatarPlaceholderComponent } from '../avatar/avatar-placeholder.component';
import { AvatarComponent } from '../avatar/avatar.component';

export type MeterTone = 'primary' | 'warn';

export interface MeterEntry {
  key: string;
  name: string;
  pictureUrl?: string | null;
  isServiceAccount?: boolean;
  placeholder?: boolean;
  percent: number;
  label: string;
  tone?: MeterTone;
}

const meterClasses: Record<MeterTone, string> = {
  primary: 'bg-primary',
  warn: 'bg-amber-500',
};

@Component({
  selector: 'app-meter-list',
  imports: [AvatarComponent, AvatarPlaceholderComponent],
  host: { class: 'flex flex-col gap-3.5', role: 'list' },
  template: `
    @for (entry of entries(); track entry.key) {
      <div
        role="listitem"
        class="grid grid-cols-[24px_minmax(0,1fr)_auto] items-center gap-3">
        @if (entry.placeholder) {
          <app-avatar-placeholder />
        } @else {
          <app-avatar
            size="sm"
            [name]="entry.name"
            [imageUrl]="entry.pictureUrl"
            [isServiceAccount]="entry.isServiceAccount ?? false" />
        }

        <div class="min-w-0">
          <div class="truncate text-sm font-medium">{{ entry.name }}</div>
          <div
            class="bg-foreground/10 mt-1.5 h-1.5 overflow-hidden rounded-full">
            <div
              class="h-full rounded-full"
              [class]="meterClasses[entry.tone ?? 'primary']"
              [style.width.%]="entry.percent"></div>
          </div>
        </div>

        <span
          class="text-muted text-right text-sm whitespace-nowrap tabular-nums">
          {{ entry.label }}
        </span>
      </div>
    }
  `,
})
export class MeterListComponent {
  readonly entries = input.required<readonly MeterEntry[]>();

  protected readonly meterClasses = meterClasses;
}
