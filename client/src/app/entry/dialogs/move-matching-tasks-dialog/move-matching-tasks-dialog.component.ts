import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject, signal } from '@angular/core';
import { LucideArrowRight } from '@lucide/angular';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { SelectableRowComponent } from '@static/components/selectable-row.component';
import { StatusChipComponent } from '@static/components/status-chip.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';

export interface MoveMatchingTask {
  id: number;
  name: string;
  systemId: string;
  groupName: string;
}

export interface MoveMatchingStatus {
  name: string;
  color?: string | null;
}

export interface MoveMatchingTasksDialogData {
  groupName: string;
  status: MoveMatchingStatus;
  previousStatus: MoveMatchingStatus | null;
  tasks: MoveMatchingTask[];
}

interface MoveMatchingTaskGroup {
  name: string;
  tasks: MoveMatchingTask[];
}

@Component({
  selector: 'app-move-matching-tasks-dialog',
  imports: [
    DialogTitleComponent,
    DialogActionsDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
    CheckboxComponent,
    SelectableRowComponent,
    StatusChipComponent,
    TaskScopeIdComponent,
    BadgeComponent,
    LucideArrowRight,
  ],
  template: `
    <app-dialog-title
      showCloseButton
      i18n="
        Title of the dialog for moving tasks that share a status. COUNT is the
        number of tasks and GROUP is the destination board group
      ">
      {data.tasks.length, plural,
        =1 {Move 1 task into {{ data.groupName }}?}
        other {Move {{ data.tasks.length }} tasks into {{ data.groupName }}?}
      }
    </app-dialog-title>

    <div class="flex w-full max-w-full min-w-0 flex-col">
      <div
        class="border-border bg-card flex flex-col gap-2.5 rounded-md border px-4 py-3.5">
        <div class="flex flex-wrap items-center gap-x-2.5 gap-y-2">
          <span
            class="text-muted text-sm"
            i18n="Label preceding the name of a board group">
            Board group
          </span>
          <span class="text-sm font-semibold">{{ data.groupName }}</span>

          @if (data.previousStatus; as previous) {
            <span class="bg-foreground/15 hidden h-3.5 w-px sm:block"></span>
            <span
              class="text-muted text-sm"
              i18n="Label preceding a from/to pair of status names">
              status changed
            </span>

            <app-status-chip [name]="previous.name" [color]="previous.color" />

            <svg
              lucideArrowRight
              class="text-muted h-3.5 w-3.5 flex-none"
              aria-hidden="true"></svg>
          } @else {
            <span class="bg-foreground/15 hidden h-3.5 w-px sm:block"></span>
            <span
              class="text-muted text-sm"
              i18n="Label preceding the status a newly created group was given">
              created with status
            </span>
          }

          <app-status-chip
            tone="primary"
            [name]="data.status.name"
            [color]="data.status.color" />
        </div>

        <p class="text-muted m-0 text-sm leading-relaxed text-pretty">
          <span
            i18n="
              Explains that tasks elsewhere on the board already carry the
              group's new status, and that accepting removes them from their
              current groups. COUNT is the number of tasks, STATUS is the new
              status name and GROUP is the destination board group
            ">
            {data.tasks.length, plural,
              =1 {1 task elsewhere on this board already has}
              other {
                {{ data.tasks.length }} tasks elsewhere on this board already
                have
              }
            }
            the status
            <span class="text-foreground font-medium">
              {{ data.status.name }}</span
            >. Moving
            {data.tasks.length, plural, =1 {it} other {them}}
            into
            <span class="text-foreground font-medium">{{
              data.groupName
            }}</span>
            takes
            {data.tasks.length, plural, =1 {it} other {them}}
            out of the
            {groups().length, plural, =1 {group} other {groups}}
            listed below.
          </span>
        </p>
      </div>

      <div
        class="border-border flex items-center justify-between gap-3 border-b px-0.5 pt-4 pb-2.5">
        <app-checkbox
          [checked]="allSelected()"
          (changed)="toggleAll($event)"
          i18n-aria-label="
            Accessible label for the checkbox that selects every listed task
          "
          aria-label="Select all tasks">
          <span class="text-sm font-medium">
            <ng-container
              i18n="
                States how many of the listed tasks are currently selected.
                SELECTED is the selected count and TOTAL is the total count
              ">
              {selectedIds().size, plural,
                =0 {None selected}
                other {
                  {{ selectedIds().size }} of {{ data.tasks.length }} selected
                }
              }
            </ng-container>
          </span>
        </app-checkbox>

        <span class="text-muted shrink-0 text-xs">
          <ng-container
            i18n="
              States how many board groups the listed tasks are spread across
            ">
            {groups().length, plural,
              =1 {Across 1 group}
              other {Across {{ groups().length }} groups}
            }
          </ng-container>
        </span>
      </div>

      <div
        class="custom-scroll flex max-h-70 flex-col gap-0.5 overflow-y-auto px-0.5 pt-2">
        @for (group of groups(); track group.name) {
          <div role="group" [attr.aria-label]="group.name">
            <div class="flex items-center gap-2.5 px-1.5 pt-2.5 pb-1.5">
              <span
                class="bg-foreground/30 h-[7px] w-[7px] flex-none rounded-full"
                aria-hidden="true"></span>
              <span
                class="text-muted font-mono text-[10px] tracking-[0.12em] uppercase">
                {{ group.name }}
              </span>
              <app-badge color="neutral">{{ group.tasks.length }}</app-badge>
              <span
                class="bg-foreground/6 h-px flex-1"
                aria-hidden="true"></span>
            </div>

            @for (task of group.tasks; track task.id) {
              <app-selectable-row
                class="grid grid-cols-[auto_auto_1fr]"
                [checked]="isSelected(task.id)"
                [attr.aria-label]="task.name"
                (toggled)="toggle(task.id, $event)">
                <app-task-scope-id [id]="task.systemId" />
                <span class="min-w-0 truncate text-sm font-medium">
                  {{ task.name }}
                </span>
              </app-selectable-row>
            }
          </div>
        }
      </div>
    </div>

    <div app-dialog-actions align="end" class="flex-col-reverse sm:flex-row">
      <button
        app-stroked-button
        color="neutral"
        type="button"
        class="w-full sm:w-auto"
        cdkFocusInitial
        (click)="close()">
        <span
          i18n="
            Dismisses the move-matching-tasks dialog, applying the status change
            but leaving every task in the group it is already in
          ">
          Leave them where they are
        </span>
      </button>
      <button
        app-flat-button
        color="primary"
        type="button"
        class="w-full sm:w-auto"
        [disabled]="selectedIds().size === 0"
        (click)="move()">
        <ng-container
          i18n="
            Button that moves the selected tasks into the chosen board group
          ">
          {selectedIds().size, plural,
            =1 {Move 1 task}
            other {Move {{ selectedIds().size }} tasks}
          }
        </ng-container>
      </button>
    </div>
  `,
})
export class MoveMatchingTasksDialogComponent {
  static readonly width = '620px';
  static readonly maxWidth = 'calc(100vw - 2rem)';

  private dialogRef =
    inject<DialogRef<number[] | undefined, MoveMatchingTasksDialogComponent>>(
      DialogRef
    );

  readonly data = inject<MoveMatchingTasksDialogData>(DIALOG_DATA);

  readonly selectedIds = signal(
    new Set(this.data.tasks.map((task) => task.id))
  );

  readonly allSelected = computed(
    () => this.selectedIds().size === this.data.tasks.length
  );

  readonly groups = computed<MoveMatchingTaskGroup[]>(() => {
    const groups = new Map<string, MoveMatchingTaskGroup>();

    for (const task of this.data.tasks) {
      const group = groups.get(task.groupName);

      if (group) {
        group.tasks.push(task);
        continue;
      }

      groups.set(task.groupName, { name: task.groupName, tasks: [task] });
    }

    return [...groups.values()];
  });

  isSelected(taskId: number) {
    return this.selectedIds().has(taskId);
  }

  toggle(taskId: number, selected: boolean) {
    this.selectedIds.update((ids) => {
      const next = new Set(ids);

      if (selected) {
        next.add(taskId);
      } else {
        next.delete(taskId);
      }

      return next;
    });
  }

  toggleAll(selected: boolean) {
    this.selectedIds.set(
      selected ? new Set(this.data.tasks.map((task) => task.id)) : new Set()
    );
  }

  move() {
    this.dialogRef.close([...this.selectedIds()]);
  }

  close() {
    this.dialogRef.close();
  }
}
