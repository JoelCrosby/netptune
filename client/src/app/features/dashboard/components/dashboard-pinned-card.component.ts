import { Component, computed, inject, signal } from '@angular/core';
import { PinnedTask, TaskPin, TaskPinScope } from '@core/models/task-pin';
import { pinnedTasksResource } from '@core/resources/task-pin.resource';
import { DialogService } from '@core/services/dialog.service';
import { PinCommandsService } from '@core/services/pin-commands.service';
import { pinScopeBadgeLabel, pinScopeIcons } from '@core/util/pin-scope';
import { TaskDetailDialogComponent } from '@entry/dialogs/task-detail-dialog/task-detail-dialog.component';
import {
  LucideDynamicIcon,
  LucideLock,
  LucidePin,
  LucidePinOff,
} from '@lucide/angular';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { PanelComponent } from '@static/components/panel.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';
import {
  SegmentedControlComponent,
  SegmentedOption,
} from '@static/components/segmented-control/segmented-control.component';
import { TaskCompactRowComponent } from '@static/components/task-compact-row.component';

type PinnedFilter = 'all' | 'yours' | 'shared';

const isPersonal = (pin: TaskPin): boolean => pin.scope === TaskPinScope.user;

@Component({
  selector: 'app-dashboard-pinned-card',
  imports: [
    BadgeComponent,
    LucideDynamicIcon,
    LucideLock,
    LucidePinOff,
    PanelComponent,
    PanelHeaderComponent,
    SegmentedControlComponent,
    TaskCompactRowComponent,
  ],
  template: `
    @if (pinnedTasks().length) {
      <app-panel>
        <app-panel-header
          [icon]="pinIcon"
          i18n-heading="Heading of the dashboard card listing pinned tasks"
          heading="Pinned"
          i18n-description="
            Description of the dashboard card listing pinned tasks
          "
          description="Tasks you and your team are keeping in view">
          <app-segmented-control
            panelHeaderActions
            [options]="filterOptions()"
            [(value)]="filter"
            i18n-ariaLabel="Accessible label for the pinned task scope filter"
            ariaLabel="Filter pinned tasks" />
        </app-panel-header>

        @for (pinned of visible(); track pinned.task.id; let first = $first) {
          <div
            class="hover:bg-foreground/3 flex items-center gap-3 pr-4 transition-colors"
            [class.border-t]="!first"
            [class.border-border]="!first">
            <app-task-compact-row
              class="min-w-0 flex-1 cursor-pointer"
              [task]="pinned.task"
              (click)="onTaskClicked(pinned)" />

            <span class="flex flex-none items-center gap-1.5">
              @for (pin of pinned.pins; track pin.id) {
                <app-badge
                  [color]="pin.scope === personalScope ? 'primary' : 'neutral'">
                  <svg
                    [lucideIcon]="scopeIcons[pin.scope]"
                    class="h-3 w-3"></svg>
                  {{ badgeLabel(pin) }}
                </app-badge>
              }
            </span>

            @if (removablePin(pinned); as pin) {
              <button
                type="button"
                class="text-foreground/35 hover:bg-foreground/8 hover:text-foreground flex h-7 w-7 flex-none cursor-pointer items-center justify-center rounded-full transition-colors"
                [title]="unpinLabel"
                [attr.aria-label]="unpinLabel"
                (click)="onUnpinClicked(pin)">
                <svg lucidePinOff class="h-3.75 w-3.75"></svg>
              </button>
            } @else {
              <span
                class="text-foreground/20 flex h-7 w-7 flex-none items-center justify-center"
                [title]="lockedLabel">
                <svg lucideLock class="h-3.5 w-3.5"></svg>
              </span>
            }
          </div>
        }
      </app-panel>
    }
  `,
})
export class DashboardPinnedCardComponent {
  private readonly pinsRef = pinnedTasksResource();
  private readonly pinCommands = inject(PinCommandsService);
  private readonly dialog = inject(DialogService);

  protected readonly pinIcon = LucidePin;
  protected readonly scopeIcons = pinScopeIcons;
  protected readonly personalScope = TaskPinScope.user;
  protected readonly unpinLabel = $localize`:Tooltip on the control that removes a pin:Unpin`;
  protected readonly lockedLabel = $localize`:Tooltip on a pin the caller is not allowed to remove:Only someone who can pin at this scope may remove it`;

  protected readonly filter = signal<PinnedFilter>('all');

  protected readonly pinnedTasks = computed(() => this.pinsRef.value() ?? []);

  private readonly yours = computed(() => {
    return this.pinnedTasks().filter((pinned) => pinned.pins.some(isPersonal));
  });

  private readonly shared = computed(() => {
    return this.pinnedTasks().filter((pinned) => {
      return pinned.pins.some((pin) => !isPersonal(pin));
    });
  });

  protected readonly visible = computed(() => {
    switch (this.filter()) {
      case 'yours':
        return this.yours();
      case 'shared':
        return this.shared();
      default:
        return this.pinnedTasks();
    }
  });

  protected readonly filterOptions = computed<SegmentedOption<PinnedFilter>[]>(
    () => {
      const all = this.pinnedTasks().length;
      const yours = this.yours().length;
      const shared = this.shared().length;

      return [
        {
          value: 'all',
          label: $localize`:Pinned task filter showing every pin. COUNT is how many:All ${all}:COUNT:`,
        },
        {
          value: 'yours',
          label: $localize`:Pinned task filter showing only your own pins. COUNT is how many:Yours ${yours}:COUNT:`,
        },
        {
          value: 'shared',
          label: $localize`:Pinned task filter showing only shared pins. COUNT is how many:Shared ${shared}:COUNT:`,
        },
      ];
    }
  );

  protected badgeLabel(pin: TaskPin) {
    return pinScopeBadgeLabel(pin.scope, pin.scopeName);
  }

  protected removablePin(pinned: PinnedTask): TaskPin | null {
    return pinned.pins.find((pin) => pin.canUnpin) ?? null;
  }

  protected onUnpinClicked(pin: TaskPin) {
    this.pinCommands.unpin(pin);
  }

  protected onTaskClicked(pinned: PinnedTask) {
    this.dialog.open(TaskDetailDialogComponent, {
      width: TaskDetailDialogComponent.width,
      data: { systemId: pinned.task.systemId },
      panelClass: 'app-modal-class',
      autoFocus: false,
    });
  }
}
