import { DialogRef } from '@angular/cdk/dialog';
import { RouterLink } from '@angular/router';
import {
  Component,
  computed,
  effect,
  inject,
  input,
  untracked,
  viewChild,
} from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { EntityType } from '@core/models/entity-type';
import { TaskPin, TaskPinScope } from '@core/models/task-pin';
import { pinnedTasksResource } from '@core/resources/task-pin.resource';
import { AiAssistantService } from '@core/services/ai-assistant.service';
import { BoardViewService } from '@core/services/board-view.service';
import { PinCommandsService } from '@core/services/pin-commands.service';
import {
  PinScopeMenuComponent,
  PinScopeTarget,
} from '@app/features/pins/components/pin-scope-menu.component';
import { ActivityMenuComponent } from '@entry/components/activity-menu/activity-menu.component';
import {
  LucideEllipsis,
  LucideExternalLink,
  LucidePin,
  LucideSparkles,
  LucideTrash2,
  LucideX,
} from '@lucide/angular';
import { SplitButtonComponent } from '@static/components/button/split-button.component';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { HEADER_ICON_BUTTON } from '../task-detail-styles';
import { TaskDetailService } from '../task-detail.service';

@Component({
  selector: 'app-task-detail-chrome',
  imports: [
    RouterLink,
    ActivityMenuComponent,
    PinScopeMenuComponent,
    SplitButtonComponent,
    DropdownMenuComponent,
    MenuItemComponent,
    TaskScopeIdComponent,
    TooltipDirective,
    LucideEllipsis,
    LucideExternalLink,
    LucideSparkles,
    LucideTrash2,
    LucideX,
  ],
  host: { class: 'flex shrink-0 items-center gap-2.5 pr-3.5 pl-5' },
  template: `
    @if (task(); as task) {
      <app-task-scope-id [id]="task.systemId" />

      @if (showBreadcrumb()) {
        <span class="text-muted truncate text-[13px]">
          {{ breadcrumb() }}
        </span>
      }

      <div class="ml-auto flex shrink-0 items-center gap-0.5">
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

        @if (showActivity() && readActivity()) {
          <app-activity-menu
            [entityType]="entityType"
            [entityId]="task.id"
            [buttonClass]="iconButtonClass" />
        }

        @if (dialogRef) {
          <a
            [class]="iconButtonClass"
            [routerLink]="['/', task.workspaceKey, 'tasks', task.systemId]"
            (click)="openFullPage($event)"
            i18n-appTooltip="Tooltip on the link that opens the task's own page"
            appTooltip="Open in full page"
            i18n-aria-label="
              Accessible label for the link that opens the task's own page
            "
            aria-label="Open in full page">
            <svg lucideExternalLink class="h-4 w-4" aria-hidden="true"></svg>
          </a>
        }

        @if (showOverflow() && hasOverflowActions()) {
          <button
            #overflowButton
            type="button"
            [class]="iconButtonClass"
            aria-haspopup="menu"
            i18n-aria-label="
              Accessible label for the button that opens the task's other
              actions
            "
            aria-label="More actions"
            (click)="overflowMenu.toggle(overflowButton)">
            <svg lucideEllipsis class="h-4 w-4"></svg>
          </button>

          <app-dropdown-menu #overflowMenu xPosition="before">
            <div class="min-w-48">
              @if (canAskAssistant()) {
                <button
                  app-menu-item
                  (click)="askAssistant(); overflowMenu.close()">
                  <svg lucideSparkles class="h-4 w-4"></svg>
                  <span i18n="Menu item that asks the assistant about a task">
                    Ask the assistant
                  </span>
                </button>
              }
              @if (canDeleteTask()) {
                <button
                  app-menu-item
                  class="text-warn!"
                  (click)="deleteTask(); overflowMenu.close()">
                  <svg lucideTrash2 class="h-4 w-4"></svg>
                  <span i18n="Menu item that deletes a task">Delete task</span>
                </button>
              }
            </div>
          </app-dropdown-menu>
        }

        @if (dialogRef) {
          <span
            class="bg-foreground/8 mx-1.5 h-5 w-px shrink-0"
            aria-hidden="true"></span>
          <button
            type="button"
            [class]="iconButtonClass"
            i18n-aria-label="
              Accessible label for the button that closes a dialog
            "
            aria-label="Close"
            (click)="dialogRef.close()">
            <svg lucideX class="h-4 w-4"></svg>
          </button>
        }
      </div>
    }
  `,
})
export class TaskDetailChromeComponent {
  readonly showBreadcrumb = input(true);
  readonly showActivity = input(true);
  readonly showOverflow = input(true);

  readonly dialogRef = inject(DialogRef, { optional: true });

  private readonly taskDetail = inject(TaskDetailService);
  private readonly pinCommands = inject(PinCommandsService);
  private readonly boardView = inject(BoardViewService);
  private readonly assistant = inject(AiAssistantService);
  private readonly pinsRef = pinnedTasksResource();
  private readonly pinButton = viewChild(SplitButtonComponent);

  readonly task = this.taskDetail.task;
  readonly entityType = EntityType.task;
  readonly iconButtonClass = HEADER_ICON_BUTTON;

  readonly pinIcon = LucidePin;
  readonly personalScope = TaskPinScope.user;
  readonly pinMenuLabel = $localize`:Accessible label for the control that opens the pin scope menu:Choose where to pin`;

  readonly readActivity = hasPermission(PERMISSIONS.activity.read);
  readonly canDeleteTask = hasPermission(PERMISSIONS.tasks.delete);

  readonly canAskAssistant = computed(() => {
    return this.assistant.isAvailable() && this.task() !== null;
  });

  readonly hasOverflowActions = computed(() => {
    return this.canDeleteTask() || this.canAskAssistant();
  });

  readonly breadcrumb = computed(() => {
    const task = this.task();

    if (!task) return '';

    return [task.projectName, task.sprintName].filter(Boolean).join(' / ');
  });

  readonly pins = computed<TaskPin[]>(() => {
    const taskId = this.task()?.id;
    const pinned = this.pinsRef.value() ?? [];

    return pinned.find((entry) => entry.task.id === taskId)?.pins ?? [];
  });

  readonly isPinned = computed(() => this.pins().length > 0);

  readonly pinLabel = computed(() => {
    if (this.isPinned()) {
      return $localize`:Label on the pin control when the task is already pinned:Pinned`;
    }

    return $localize`:Label on the control that pins a task:Pin`;
  });

  readonly pinTarget = computed<PinScopeTarget>(() => {
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
    effect(() => {
      this.pinCommands.scopeMenuRequested();

      untracked(() => this.pinButton()?.openMenu());
    });
  }

  /* A modified click is the browser's to handle: it opens a tab of its own and
     the dialog stays put. Only a plain click hands the task over to its page. */
  protected openFullPage(event: MouseEvent) {
    if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) {
      return;
    }

    this.dialogRef?.close();
  }

  protected askAssistant() {
    const task = this.task();

    if (!task) return;

    this.assistant.askAboutTask(task);
  }

  protected deleteTask() {
    this.taskDetail.deleteTask();
  }

  protected onPinScopeToggled(scope: TaskPinScope) {
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

  protected onUnpinAll() {
    this.pinCommands.unpinEverywhere(this.pins());
  }

  private scopeEntityId(scope: TaskPinScope) {
    const target = this.pinTarget();

    if (scope === TaskPinScope.board) return target.boardId;
    if (scope === TaskPinScope.project) return target.projectId;

    return null;
  }
}
