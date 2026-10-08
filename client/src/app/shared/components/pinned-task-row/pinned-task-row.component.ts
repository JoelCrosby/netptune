import { Component, computed, input, output } from '@angular/core';
import { PinnedTask, TaskPin, TaskPinScope } from '@core/models/task-pin';
import { pinScopeBadgeLabel, pinScopeIcons } from '@core/util/pin-scope';
import { RouterLink } from '@angular/router';
import {
  LucideDynamicIcon,
  LucideExternalLink,
  LucideLock,
  LucidePinOff,
} from '@lucide/angular';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { TaskCompactRowComponent } from '@static/components/task-compact-row.component';
import { TaskPriorityComponent } from '@static/components/task-priority.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';

export type PinnedTaskRowLayout = 'full' | 'compact';

// One pinned task: the task itself, a badge per pin naming who it is pinned for, and
// the control to unpin it. `full` carries the task's status, estimate and assignees;
// `compact` keeps to the key, name and priority so a narrow list still reads the name.
@Component({
  selector: 'app-pinned-task-row',
  imports: [
    BadgeComponent,
    IconButtonComponent,
    LucideDynamicIcon,
    LucideExternalLink,
    LucideLock,
    LucidePinOff,
    RouterLink,
    TaskCompactRowComponent,
    TaskPriorityComponent,
    TooltipDirective,
  ],
  host: {
    class:
      'hover:bg-foreground/3 flex items-center gap-3 pr-4 transition-colors',
  },
  template: `
    @if (layout() === 'full') {
      <app-task-compact-row
        class="min-w-0 flex-1 cursor-pointer"
        [task]="pinned().task"
        (click)="opened.emit()" />
    } @else {
      <button
        type="button"
        class="flex min-w-0 flex-1 cursor-pointer items-center gap-3 py-2.5 pl-4 text-left"
        (click)="opened.emit()">
        <span class="font-avatar text-muted w-16 shrink-0 text-xs">
          {{ pinned().task.systemId }}
        </span>
        <span class="min-w-0 flex-1 truncate text-sm">
          {{ pinned().task.name }}
        </span>
        @if (pinned().task.priority !== null) {
          <app-task-priority
            size="small"
            class="shrink-0"
            [priority]="pinned().task.priority" />
        }
      </button>
    }

    <span class="flex flex-none items-center gap-1.5">
      @for (pin of pinned().pins; track pin.id) {
        <app-badge
          [color]="pin.scope === personalScope ? 'primary' : 'neutral'">
          <svg [lucideIcon]="scopeIcons[pin.scope]" class="h-3 w-3"></svg>
          {{ badgeLabel(pin) }}
        </app-badge>
      }
    </span>

    @if (showPageLink()) {
      <a
        class="text-foreground/35 hover:text-foreground hover:bg-hover flex h-7 w-7 flex-none items-center justify-center rounded-full transition-colors"
        [routerLink]="[
          '/',
          pinned().task.workspaceKey,
          'tasks',
          pinned().task.systemId,
        ]"
        i18n-appTooltip="Tooltip on the link that opens the task's own page"
        appTooltip="Open in full page"
        i18n-aria-label="
          Accessible label for the link that opens the task's own page
        "
        aria-label="Open in full page">
        <svg lucideExternalLink class="h-3.75 w-3.75" aria-hidden="true"></svg>
      </a>
    }

    @if (removablePin(); as pin) {
      <button
        type="button"
        app-icon-button
        class="text-foreground/35 hover:text-foreground h-7 w-7 flex-none"
        [title]="unpinLabel"
        [attr.aria-label]="unpinLabel"
        (click)="unpinned.emit(pin)">
        <svg lucidePinOff class="h-3.75 w-3.75"></svg>
      </button>
    } @else {
      <span
        class="text-foreground/20 flex h-7 w-7 flex-none items-center justify-center"
        [title]="lockedLabel">
        <svg lucideLock class="h-3.5 w-3.5"></svg>
      </span>
    }
  `,
})
export class PinnedTaskRowComponent {
  readonly pinned = input.required<PinnedTask>();
  readonly layout = input<PinnedTaskRowLayout>('full');
  readonly showPageLink = input(false);

  readonly opened = output();
  readonly unpinned = output<TaskPin>();

  protected readonly scopeIcons = pinScopeIcons;
  protected readonly personalScope = TaskPinScope.user;
  protected readonly unpinLabel = $localize`:Tooltip on the control that removes a pin:Unpin`;
  protected readonly lockedLabel = $localize`:Tooltip on a pin the caller is not allowed to remove:Only someone who can pin at this scope may remove it`;

  protected readonly removablePin = computed(() => {
    return this.pinned().pins.find((pin) => pin.canUnpin) ?? null;
  });

  protected badgeLabel(pin: TaskPin) {
    return pinScopeBadgeLabel(pin.scope, pin.scopeName);
  }
}
