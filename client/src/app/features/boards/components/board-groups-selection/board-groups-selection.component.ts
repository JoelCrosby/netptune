import { Component, computed, inject } from '@angular/core';
import { netptunePermissions } from '@app/core/auth/permissions';
import { selectPermissions } from '@app/core/store/auth/auth.selectors';
import {
  clearTaskSelection,
  deleteSelectedTasks,
} from '@app/core/store/groups/board-groups.actions';
import {
  selectSelectedTasks,
  selectSelectedTasksCount,
} from '@app/core/store/groups/board-groups.selectors';
import { DialogService } from '@core/services/dialog.service';
import {
  LucideCombine,
  LucideDynamicIcon,
  LucideEllipsis,
  LucideListX,
  LucideTrash2,
  LucideUsers,
} from '@lucide/angular';
import { Store } from '@ngrx/store';
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
          appTooltip="Clear Task Selection">
          <strong>{{ count + ' ' }}</strong>
          <span>tasks selected</span>
          <svg lucideListX size="18" class="close-btn"></svg>
        </button>

        @if (actions().length) {
          <span #trigger>
            <app-filter-action-button
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
  private store = inject(Store);
  private dialog = inject(DialogService);

  readonly lucideEllipsis = LucideEllipsis;

  selected = this.store.selectSignal(selectSelectedTasks);
  count = this.store.selectSignal(selectSelectedTasksCount);
  permissions = this.store.selectSignal(selectPermissions);

  actions = computed(() => {
    const actions = [];
    const permissions = this.permissions();

    if (permissions.has(netptunePermissions.tasks.delete)) {
      actions.push({
        label: 'Delete tasks',
        action: this.onDeleteClicked.bind(this),
        icon: LucideTrash2,
      });
    }
    if (permissions.has(netptunePermissions.tasks.move)) {
      actions.push({
        label: 'Move to group',
        action: this.onMoveTasksClicked.bind(this),
        icon: LucideCombine,
      });
    }
    if (permissions.has(netptunePermissions.tasks.reassign)) {
      actions.push({
        label: 'Reassign',
        action: this.onReassignTasksClicked.bind(this),
        icon: LucideUsers,
      });
    }

    return actions;
  });

  onClearClicked() {
    this.store.dispatch(clearTaskSelection());
  }

  onDeleteClicked() {
    this.store.dispatch(deleteSelectedTasks());
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
