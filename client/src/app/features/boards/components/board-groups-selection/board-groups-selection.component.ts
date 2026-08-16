import { Component, computed, inject } from '@angular/core';
import { SessionService } from '@core/services/session.service';
import { PERMISSIONS } from '@app/core/auth/permissions';
import { BoardGroupCommandsService } from '@core/services/board-group-commands.service';
import { BoardSelectionService } from '@core/services/board-selection.service';
import { DialogService } from '@core/services/dialog.service';
import {
  LucideCombine,
  LucideDynamicIcon,
  LucideEllipsis,
  LucideListX,
  LucideTrash2,
  LucideUsers,
} from '@lucide/angular';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { FilterActionButtonComponent } from '@static/components/filter-action-button/filter-action-button.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { MoveTasksDialogComponent } from '../move-tasks-dialog/move-tasks-dialog.component';
import { ReassignTasksDialogComponent } from '../reassign-tasks-dialog/reassign-tasks-dialog.component';

@Component({
  selector: 'app-board-groups-selection',
  imports: [
    TooltipDirective,
    LucideListX,
    LucideDynamicIcon,
    FilterActionButtonComponent,
    DropdownMenuComponent,
    MenuItemComponent,
  ],
  template: `
    @if (count(); as count) {
      <div
        class="border-primary/40 bg-primary/10 text-primary flex flex-row items-center gap-1 rounded-lg border px-1 py-0.5">
        <button
          class="hover:bg-primary/20 flex h-8 cursor-pointer appearance-none flex-row items-center gap-2 rounded-sm px-4 transition-[background-color,color] duration-140 ease-in-out outline-none"
          (click)="onClearClicked()"
          i18n-appTooltip="Tooltip on the button that clears the task selection"
          appTooltip="Clear Task Selection">
          <ng-container i18n="Count of selected tasks shown above the board">
            {count, plural,
              =1 {<strong>1</strong> task selected}
              other {<strong>{{ count }}</strong> tasks selected}
            }
          </ng-container>
          <svg lucideListX size="18" class="close-btn"></svg>
        </button>

        @if (actions().length) {
          <span #trigger>
            <app-filter-action-button
              i18n-label="Button that opens actions for the selected tasks"
              label="Task actions"
              [icon]="lucideEllipsis"
              (action)="menu.toggle(trigger)" />
          </span>

          <app-dropdown-menu #menu xPosition="before">
            @for (action of actions(); track action.label) {
              <button app-menu-item (click)="menu.close(); action.action()">
                <svg [lucideIcon]="action.icon" class="h-4 w-4"></svg>
                <span>{{ action.label }}</span>
              </button>
            }
          </app-dropdown-menu>
        }
      </div>
    }
  `,
})
export class BoardGroupsSelectionComponent {
  private dialog = inject(DialogService);
  private selection = inject(BoardSelectionService);
  private boardCommands = inject(BoardGroupCommandsService);

  readonly lucideEllipsis = LucideEllipsis;

  selected = this.selection.taskIds;
  count = this.selection.count;
  permissions = inject(SessionService).permissions;

  actions = computed(() => {
    const actions = [];
    const permissions = this.permissions();

    if (permissions.has(PERMISSIONS.tasks.delete)) {
      actions.push({
        label: $localize`:Action that deletes the selected tasks:Delete tasks`,
        action: this.onDeleteClicked.bind(this),
        icon: LucideTrash2,
      });
    }
    if (permissions.has(PERMISSIONS.tasks.move)) {
      actions.push({
        label: $localize`:Action that moves the selected tasks to another board group:Move to group`,
        action: this.onMoveTasksClicked.bind(this),
        icon: LucideCombine,
      });
    }
    if (permissions.has(PERMISSIONS.tasks.reassign)) {
      actions.push({
        label: $localize`:Action that reassigns the selected tasks to another person:Reassign`,
        action: this.onReassignTasksClicked.bind(this),
        icon: LucideUsers,
      });
    }

    return actions;
  });

  onClearClicked() {
    this.selection.clear();
  }

  onDeleteClicked() {
    this.boardCommands.deleteSelectedTasks();
  }

  onMoveTasksClicked() {
    this.dialog.open(MoveTasksDialogComponent, {
      width: '600px',
      panelClass: 'app-modal-class',
    });
  }

  onReassignTasksClicked() {
    this.dialog.open(ReassignTasksDialogComponent, {
      width: '400px',
      panelClass: 'app-modal-class',
    });
  }
}
