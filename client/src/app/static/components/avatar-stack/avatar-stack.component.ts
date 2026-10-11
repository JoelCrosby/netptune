import { Component, computed, input } from '@angular/core';
import { TooltipDirective } from '../../directives/tooltip.directive';
import { AvatarComponent } from '../avatar/avatar.component';

export interface AvatarStackItem {
  id: string;
  displayName: string;
  pictureUrl?: string | null;
  isServiceAccount?: boolean;
  online?: boolean;
  label?: string;
}

@Component({
  selector: 'app-avatar-stack',
  imports: [AvatarComponent, TooltipDirective],
  host: {
    class: 'flex max-w-32 items-center -space-x-2 overflow-hidden',
  },
  template: `
    @for (avatar of shown(); track avatar.id; let index = $index) {
      <span
        class="relative inline-flex rounded-full"
        [style.z-index]="shown().length - index"
        [appTooltip]="avatar.label ?? avatar.displayName">
        <app-avatar
          class="ring-card rounded-full ring-2"
          size="sm"
          [tooltip]="false"
          [name]="avatar.displayName"
          [imageUrl]="avatar.pictureUrl"
          [isServiceAccount]="avatar.isServiceAccount ?? false" />
        @if (avatar.online) {
          <span
            class="border-card pointer-events-none absolute right-0 bottom-0 h-2.5 w-2.5 rounded-full border-2 bg-green-500"></span>
        }
      </span>
    }
    @if (hiddenCount() > 0) {
      <span
        class="text-foreground/80 ring-card relative z-10 flex h-6 min-w-6 items-center justify-center rounded-full bg-neutral-200 px-1 text-[11px] font-medium ring-2 dark:bg-neutral-700"
        [appTooltip]="hiddenLabel()"
        >+{{ hiddenCount() }}</span
      >
    }
  `,
})
export class AvatarStackComponent {
  readonly avatars = input.required<readonly AvatarStackItem[]>();
  readonly limit = input<number | null>(null);

  protected readonly shown = computed(() => {
    const limit = this.limit();
    const avatars = this.avatars();

    return limit === null ? avatars : avatars.slice(0, limit);
  });

  protected readonly hiddenCount = computed(() => {
    return this.avatars().length - this.shown().length;
  });

  protected readonly hiddenLabel = computed(() => {
    return this.avatars()
      .slice(this.shown().length)
      .map((avatar) => avatar.label ?? avatar.displayName)
      .join('\n');
  });
}
