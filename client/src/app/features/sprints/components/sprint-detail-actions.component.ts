import {
  Component,
  ElementRef,
  computed,
  inject,
  input,
  output,
  viewChild,
} from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { SprintStatus } from '@core/enums/sprint-status';
import { NotificationScope } from '@core/models/notification-subscription';
import { SprintDetailViewModel } from '@core/models/view-models/sprint-detail-view-model';
import { AiAssistantService } from '@core/services/ai-assistant.service';
import { SprintCommandsService } from '@core/services/sprint-commands.service';
import {
  LucideBell,
  LucideCheck,
  LucideChevronDown,
  LucideEllipsis,
  LucideListPlus,
  LucidePlus,
  LucideSettings2,
  LucideSparkles,
  LucideTrash2,
} from '@lucide/angular';
import { NotificationSubscribeComponent } from '@shared/components/notification-subscribe/notification-subscribe.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { MenuSeparatorComponent } from '@static/components/dropdown-menu/menu-separator.component';

// The sprint header used to carry six controls of equal weight. They collapse here into three: the
// two ways of putting work into the sprint group behind one `Add work` menu, the one action the
// sprint is currently waiting on stays a button, and everything else moves into the overflow.
@Component({
  selector: 'app-sprint-detail-actions',
  imports: [
    DropdownMenuComponent,
    FlatButtonComponent,
    LucideBell,
    LucideCheck,
    LucideChevronDown,
    LucideEllipsis,
    LucideListPlus,
    LucidePlus,
    LucideSettings2,
    LucideSparkles,
    LucideTrash2,
    MenuItemComponent,
    MenuSeparatorComponent,
    NotificationSubscribeComponent,
    StrokedButtonComponent,
  ],
  host: { class: 'flex shrink-0 flex-wrap items-center gap-2' },
  template: `
    @if (canAddWork()) {
      <button
        app-stroked-button
        color="neutral"
        type="button"
        aria-haspopup="menu"
        (click)="addMenu.toggle($any($event.currentTarget))">
        <svg lucidePlus class="h-4 w-4 shrink-0"></svg>
        <span i18n="Button that opens the menu for adding work to the sprint">
          Add work
        </span>
        <svg lucideChevronDown class="h-4 w-4 shrink-0 opacity-70"></svg>
      </button>

      <app-dropdown-menu #addMenu xPosition="before" panelClass="w-64 p-1">
        <button
          app-menu-item
          type="button"
          class="items-start"
          (click)="createTask.emit(); addMenu.close()">
          <svg lucidePlus class="text-muted mt-0.5 h-4 w-4 shrink-0"></svg>
          <span class="min-w-0">
            <span
              class="block font-medium"
              i18n="Menu item that creates a new task in the sprint">
              Create Sprint Task
            </span>
            <span
              class="text-muted block text-xs"
              i18n="Explains what creating a sprint task does">
              New task, added to this sprint
            </span>
          </span>
        </button>

        <button
          app-menu-item
          type="button"
          class="items-start"
          (click)="assignTasks.emit(); addMenu.close()">
          <svg lucideListPlus class="text-muted mt-0.5 h-4 w-4 shrink-0"></svg>
          <span class="min-w-0">
            <span
              class="block font-medium"
              i18n="
                Menu item that opens the dialog for adding existing tasks to the
                sprint
              ">
              Assign Existing Tasks
            </span>
            <span
              class="text-muted block text-xs"
              i18n="Explains where assigned tasks come from">
              Pull from the backlog
            </span>
          </span>
        </button>
      </app-dropdown-menu>
    }

    @if (canUpdate() && sprint().status === sprintStatus.planning) {
      <button
        app-flat-button
        color="primary"
        type="button"
        [disabled]="updateLoading()"
        (click)="startSprint.emit()">
        <span i18n="Button that starts the sprint">Start Sprint</span>
      </button>
    }

    @if (canUpdate() && sprint().status === sprintStatus.active) {
      <button
        app-flat-button
        color="primary"
        type="button"
        [disabled]="updateLoading()"
        (click)="completeSprint.emit()">
        <svg lucideCheck class="h-4 w-4 shrink-0"></svg>
        <span i18n="Button that completes the sprint">Complete Sprint</span>
      </button>
    }

    <button
      #moreTrigger
      app-stroked-button
      color="neutral"
      type="button"
      class="w-10 min-w-0 px-0"
      aria-haspopup="menu"
      i18n-aria-label="
        Accessible label for the button that opens the sprint action menu
      "
      aria-label="More sprint actions"
      (click)="moreMenu.toggle($any($event.currentTarget))">
      <svg lucideEllipsis class="h-4 w-4 shrink-0"></svg>
    </button>

    <app-dropdown-menu #moreMenu xPosition="before" panelClass="w-60 p-1">
      <button
        app-menu-item
        type="button"
        (click)="moreMenu.close(); notify.open(notifyOrigin().nativeElement)">
        <svg lucideBell class="h-4 w-4 shrink-0"></svg>
        <span i18n="Menu item that opens the sprint notification settings">
          Notify me about this sprint
        </span>
      </button>

      @if (assistant.isAvailable()) {
        <button
          app-menu-item
          type="button"
          (click)="assistant.askAboutSprint(sprint()); moreMenu.close()">
          <svg lucideSparkles class="h-4 w-4 shrink-0"></svg>
          <span i18n="Menu item that asks the assistant about this sprint">
            Summarise with Assistant
          </span>
        </button>
      }

      @if (canUpdate()) {
        <button
          app-menu-item
          type="button"
          (click)="editSprint.emit(); moreMenu.close()">
          <svg lucideSettings2 class="h-4 w-4 shrink-0"></svg>
          <span i18n="Menu item that edits the sprint">Sprint settings</span>
        </button>

        <app-menu-separator />

        <button
          app-menu-item
          type="button"
          color="warn"
          (click)="deleteSprint.emit(); moreMenu.close()">
          <svg lucideTrash2 class="h-4 w-4 shrink-0"></svg>
          <span i18n="Menu item that deletes the sprint">Delete sprint</span>
        </button>
      }
    </app-dropdown-menu>

    <app-notification-subscribe
      #notify
      appearance="hidden"
      xPosition="before"
      [scope]="notificationScope.sprint"
      [scopeEntityId]="sprint().id"
      [scopeName]="sprint().name" />
  `,
})
export class SprintDetailActionsComponent {
  readonly sprint = input.required<SprintDetailViewModel>();

  readonly createTask = output();
  readonly assignTasks = output();
  readonly startSprint = output();
  readonly completeSprint = output();
  readonly editSprint = output();
  readonly deleteSprint = output();

  protected readonly assistant = inject(AiAssistantService);

  // The notify panel hangs off the overflow trigger the row was reached through, so it opens where
  // the menu it replaces just was.
  protected readonly notifyOrigin = viewChild.required('moreTrigger', {
    read: ElementRef<HTMLElement>,
  });

  protected readonly sprintStatus = SprintStatus;
  protected readonly notificationScope = NotificationScope;

  protected readonly updateLoading = inject(SprintCommandsService).isUpdating;
  protected readonly canUpdate = hasPermission(PERMISSIONS.sprints.update);

  private readonly canManageTasks = hasPermission(
    PERMISSIONS.sprints.manageTasks
  );

  protected readonly canAddWork = computed(() => {
    return (
      this.canManageTasks() && this.sprint().status !== SprintStatus.completed
    );
  });
}
