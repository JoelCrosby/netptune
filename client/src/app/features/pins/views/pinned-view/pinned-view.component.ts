import { Component, computed, inject } from '@angular/core';
import { TaskPin, TaskPinScope } from '@core/models/task-pin';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { pinnedTasksResource } from '@core/resources/task-pin.resource';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { DialogService } from '@core/services/dialog.service';
import { PinCommandsService } from '@core/services/pin-commands.service';
import {
  pinScopeGroupKind,
  pinScopeGroupName,
  pinScopeIcons,
  pinScopeVisibilityNote,
} from '@core/util/pin-scope';
import { TaskDetailDialogComponent } from '@entry/dialogs/task-detail-dialog/task-detail-dialog.component';
import {
  LucideLock,
  LucidePinOff,
  type LucideIconInput,
} from '@lucide/angular';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { IconCircleComponent } from '@static/components/icon-circle.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { PanelComponent } from '@static/components/panel.component';
import { TaskCompactRowComponent } from '@static/components/task-compact-row.component';

interface PinnedRow {
  pin: TaskPin;
  task: TaskViewModel;
}

interface PinGroup {
  key: string;
  icon: LucideIconInput;
  name: string;
  kind: string | null;
  note: string;
  rows: PinnedRow[];
}

@Component({
  selector: 'app-pinned-view',
  imports: [
    BadgeComponent,
    EmptyStateComponent,
    IconCircleComponent,
    LucideLock,
    LucidePinOff,
    PageContainerComponent,
    PageHeaderComponent,
    PanelComponent,
    TaskCompactRowComponent,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header
        i18n-title="Page title for the pinned task list"
        title="Pinned"
        [count]="count()" />

      @if (groups().length) {
        <app-panel>
          @for (group of groups(); track group.key; let first = $first) {
            <div
              class="border-border bg-foreground/3 flex flex-wrap items-center justify-between gap-3 border-b px-4 py-2.5"
              [class.border-t]="!first">
              <span class="flex items-center gap-2.5">
                <app-icon-circle size="small" [icon]="group.icon" />
                <span class="text-[13px] font-medium">{{ group.name }}</span>
                @if (group.kind) {
                  <app-badge>{{ group.kind }}</app-badge>
                }
                <span class="text-foreground/35 text-xs tabular-nums">
                  {{ group.rows.length }}
                </span>
              </span>
              <span class="text-muted text-xs">{{ group.note }}</span>
            </div>

            @for (row of group.rows; track row.pin.id; let firstRow = $first) {
              <div
                class="hover:bg-foreground/3 flex items-center gap-3 pr-4 transition-colors"
                [class.border-t]="!firstRow"
                [class.border-border]="!firstRow">
                <app-task-compact-row
                  class="min-w-0 flex-1 cursor-pointer"
                  [task]="row.task"
                  (click)="onTaskClicked(row.task)" />

                @if (row.pin.canUnpin) {
                  <button
                    type="button"
                    class="text-foreground/35 hover:bg-foreground/8 hover:text-foreground flex h-7 w-7 flex-none cursor-pointer items-center justify-center rounded-full transition-colors"
                    [title]="unpinLabel"
                    [attr.aria-label]="unpinLabel"
                    (click)="onUnpinClicked(row.pin)">
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
          }
        </app-panel>
      } @else if (!loading()) {
        <app-empty-state
          i18n-title="Empty state title for the pinned task list"
          title="Nothing pinned"
          i18n-description="Empty state message for the pinned task list"
          description="Pin a task from a board or its detail view to keep it in reach." />
      }
    </app-page-container>
  `,
})
export class PinnedViewComponent {
  private readonly pinsRef = pinnedTasksResource();
  private readonly pinCommands = inject(PinCommandsService);
  private readonly dialog = inject(DialogService);
  private readonly workspace = inject(CurrentWorkspaceService).workspace;

  protected readonly unpinLabel = $localize`:Tooltip on the control that removes a pin:Unpin`;
  protected readonly lockedLabel = $localize`:Tooltip on a pin the caller is not allowed to remove:Only someone who can pin at this scope may remove it`;

  private readonly pinnedTasks = computed(() => this.pinsRef.value() ?? []);

  protected readonly loading = computed(() => this.pinsRef.isLoading());
  protected readonly count = computed(() => this.pinnedTasks().length);

  protected readonly groups = computed<PinGroup[]>(() => {
    const workspaceName = this.workspace()?.name ?? '';
    const groups = new Map<string, PinGroup>();

    for (const pinned of this.pinnedTasks()) {
      for (const pin of pinned.pins) {
        const key = `${pin.scope}:${pin.scopeEntityId}`;
        const group = groups.get(key) ?? this.newGroup(key, pin, workspaceName);

        group.rows.push({ pin, task: pinned.task });
        groups.set(key, group);
      }
    }

    for (const group of groups.values()) {
      group.rows.sort(
        (left, right) => left.pin.sortOrder - right.pin.sortOrder
      );
    }

    return [...groups.values()].sort(byScopeThenName);
  });

  protected onUnpinClicked(pin: TaskPin) {
    this.pinCommands.unpin(pin);
  }

  protected onTaskClicked(task: TaskViewModel) {
    this.dialog.open(TaskDetailDialogComponent, {
      width: TaskDetailDialogComponent.width,
      data: { systemId: task.systemId },
      panelClass: 'app-modal-class',
      autoFocus: false,
    });
  }

  private newGroup(key: string, pin: TaskPin, workspaceName: string): PinGroup {
    return {
      key,
      icon: pinScopeIcons[pin.scope],
      name: pinScopeGroupName(pin.scope, pin.scopeName),
      kind: pinScopeGroupKind(pin.scope),
      note: pinScopeVisibilityNote(pin.scope, workspaceName),
      rows: [],
    };
  }
}

const scopeOrder: Record<TaskPinScope, number> = {
  [TaskPinScope.user]: 0,
  [TaskPinScope.board]: 1,
  [TaskPinScope.project]: 2,
  [TaskPinScope.workspace]: 3,
};

function byScopeThenName(left: PinGroup, right: PinGroup): number {
  const leftScope = scopeOrder[left.rows[0].pin.scope];
  const rightScope = scopeOrder[right.rows[0].pin.scope];

  if (leftScope !== rightScope) return leftScope - rightScope;

  return left.name.localeCompare(right.name);
}
