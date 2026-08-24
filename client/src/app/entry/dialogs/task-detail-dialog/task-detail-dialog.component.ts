import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import {
  Component,
  computed,
  effect,
  inject,
  untracked,
  viewChild,
} from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { SpinnerComponent } from '@app/static/components/spinner/spinner.component';
import { EntityType } from '@core/models/entity-type';
import { StatusCategory } from '@core/models/status';
import { ActivityMenuComponent } from '@entry/components/activity-menu/activity-menu.component';
import { LucideCheck, LucidePin } from '@lucide/angular';
import { TaskPin, TaskPinScope } from '@core/models/task-pin';
import { pinnedTasksResource } from '@core/resources/task-pin.resource';
import { BoardViewService } from '@core/services/board-view.service';
import { PinCommandsService } from '@core/services/pin-commands.service';
import {
  PinScopeMenuComponent,
  PinScopeTarget,
} from '@app/features/pins/components/pin-scope-menu.component';
import { SplitButtonComponent } from '@static/components/button/split-button.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { SprintBadgeComponent } from '@static/components/sprint-badge.component';
import { TaskDates } from '@static/components/task-dates/task-dates.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { TaskDetailActionsComponent } from './task-detail-actions.component';
import { TaskDetailCommentsComponent } from './task-detail-comments.component';
import { TaskDetailDescriptionComponent } from './task-detail-description.component';
import { TaskDetailHeaderComponent } from './task-detail-header.component';
import { TaskDetailPropertiesComponent } from './task-detail-properties.component';
import { TaskDetailRelationsComponent } from './task-detail-relations.component';
import { TaskDetailTagsComponent } from './task-detail-tags.component';
import { TaskDetailService } from './task-detail.service';
import { PERMISSIONS } from '@app/core/auth/permissions';
import { TaskDetailFilesComponent } from './task-detail-files.component';
import { TaskDetailFlagsComponent } from './task-detail-flags.component';

export interface TaskDetailDialogData {
  systemId: string;
}

@Component({
  selector: 'app-task-detail-dialog',
  template: `
    @if (task(); as task) {
      <div>
        <div
          class="mb-1 flex flex-row items-center justify-between gap-4 pr-6 pl-2">
          <app-task-detail-header />

          <div class="flex items-center gap-4">
            @if (task.sprintName) {
              <app-sprint-badge
                [name]="task.sprintName"
                [status]="task.sprintStatus" />
            }
            @if (task.statusCategory === statusCategory.done) {
              <svg lucideCheck class="h-4 w-4 text-green-500"></svg>
            }
            <app-task-scope-id [id]="task.systemId" />
            <app-split-button
              #pinButton
              [icon]="pinIcon"
              [label]="pinLabel()"
              [menuLabel]="pinMenuLabel"
              [iconFilled]="isPinned()"
              [pressed]="isPinned()"
              (activated)="onPinScopeToggled(personalScope)">
              <app-pin-scope-menu
                class="w-72"
                [pins]="pins()"
                [target]="pinTarget()"
                (toggled)="onPinScopeToggled($event); pinButton.closeMenu()"
                (unpinAll)="onUnpinAll(); pinButton.closeMenu()" />
            </app-split-button>
            @if (readActivity()) {
              <app-activity-menu
                [entityType]="entityType"
                [entityId]="task.id" />
            }
          </div>
        </div>

        <div class="flex flex-row gap-12 px-6">
          <div class="flex w-64 grow flex-col">
            @if (readTags()) {
              <app-task-detail-tags />
            }

            @if (readFlags()) {
              <app-task-detail-flags />
            }

            <app-task-detail-description />
            @if (readFiles()) {
              <app-task-detail-files [systemId]="task.systemId" />
            }
            <app-task-detail-relations />
            @if (readComments()) {
              <app-task-detail-comments />
            }
          </div>

          <div class="bg-card/40 mt-4 flex flex-col gap-6 rounded px-6 pb-6">
            <app-task-detail-properties />
            <app-task-detail-actions />
          </div>
        </div>
      </div>
      <div app-dialog-actions align="start">
        <app-task-dates [task]="task" />
      </div>
    } @else {
      <div class="flex h-243.5 flex-col items-center justify-center">
        <app-spinner diameter="64" />
      </div>
    }
  `,
  imports: [
    LucideCheck,
    ActivityMenuComponent,
    PinScopeMenuComponent,
    SplitButtonComponent,
    DialogActionsDirective,
    SpinnerComponent,
    SprintBadgeComponent,
    TaskDates,
    TaskScopeIdComponent,
    TaskDetailPropertiesComponent,
    TaskDetailHeaderComponent,
    TaskDetailDescriptionComponent,
    TaskDetailRelationsComponent,
    TaskDetailCommentsComponent,
    TaskDetailTagsComponent,
    TaskDetailActionsComponent,
    TaskDetailFilesComponent,
    TaskDetailFlagsComponent,
  ],
  providers: [TaskDetailService],
})
export class TaskDetailDialogComponent {
  data = inject<TaskDetailDialogData>(DIALOG_DATA, { optional: false });
  private dialogRef = inject<DialogRef<TaskDetailDialogComponent>>(DialogRef);
  private snackbar = inject(SnackbarService);
  private taskDetail = inject(TaskDetailService);
  private pinCommands = inject(PinCommandsService);
  private boardView = inject(BoardViewService);
  private pinsRef = pinnedTasksResource();
  private pinButton = viewChild(SplitButtonComponent);

  public static width = '972px';

  entityType = EntityType.task;
  statusCategory = StatusCategory;

  task = this.taskDetail.task;

  readTags = hasPermission(PERMISSIONS.tags.read);

  readFiles = hasPermission(PERMISSIONS.files.read);

  readFlags = hasPermission(PERMISSIONS.flags.read);

  readComments = hasPermission(PERMISSIONS.comments.read);

  onPinScopeToggled(scope: TaskPinScope) {
    const taskId = this.task()?.id;

    if (!taskId) return;

    const existing = this.pins().find((pin) => pin.scope === scope);

    if (existing) {
      this.pinCommands.unpin(existing);

      return;
    }

    this.pinCommands.pin({
      taskId,
      scope,
      scopeEntityId: this.scopeEntityId(scope),
    });
  }

  onUnpinAll() {
    this.pinCommands.unpinEverywhere(this.pins());
  }

  private scopeEntityId(scope: TaskPinScope) {
    const target = this.pinTarget();

    if (scope === TaskPinScope.board) return target.boardId;
    if (scope === TaskPinScope.project) return target.projectId;

    return null;
  }

  readActivity = hasPermission(PERMISSIONS.activity.read);

  pinIcon = LucidePin;
  personalScope = TaskPinScope.user;
  pinMenuLabel = $localize`:Accessible label for the control that opens the pin scope menu:Choose where to pin`;

  pins = computed<TaskPin[]>(() => {
    const taskId = this.task()?.id;
    const pinned = this.pinsRef.value() ?? [];

    return pinned.find((entry) => entry.task.id === taskId)?.pins ?? [];
  });

  isPinned = computed(() => this.pins().length > 0);

  pinLabel = computed(() => {
    if (this.isPinned()) {
      return $localize`:Label on the pin control when the task is already pinned:Pinned`;
    }

    return $localize`:Label on the control that pins a task:Pin`;
  });

  pinTarget = computed<PinScopeTarget>(() => {
    const task = this.task();
    const board = this.boardView.board();

    return {
      boardId: board?.id ?? null,
      boardName: board?.name ?? null,
      projectId: task?.projectId ?? null,
      projectName: task?.projectName ?? null,
    };
  });

  constructor() {
    this.taskDetail.show(this.data.systemId);

    effect(() => {
      this.pinCommands.scopeMenuRequested();

      untracked(() => this.pinButton()?.openMenu());
    });

    effect(() => {
      const wasRemoved = this.taskDetail.loadError()?.status === 404;

      if (!wasRemoved) return;

      untracked(() => {
        this.snackbar.error(
          $localize`:Shown when the open task no longer exists:This task no longer exists.`
        );
        this.dialogRef.close();
      });
    });
  }
}
