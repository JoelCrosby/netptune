import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
} from '@angular/core';
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
  LucideListX,
  LucideMove,
  LucideTrash2,
  LucideUsers,
} from '@lucide/angular';
import { Store } from '@ngrx/store';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { BoardGroupHeaderActionComponent } from '../board-group-header/board-group-header-action.component';
import { BoardGroupHeaderSeperatorComponent } from '../board-group-header/board-group-header-seperator.component';
import { MoveTasksDialogComponent } from '../move-tasks-dialog/move-tasks-dialog.component';
import { ReassignTasksDialogComponent } from '../reassign-tasks-dialog/reassign-tasks-dialog.component';

@Component({
  selector: 'app-board-groups-selection',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    TooltipDirective,
    LucideListX,
    BoardGroupHeaderSeperatorComponent,
    BoardGroupHeaderActionComponent,
  ],
  template: `
    @if (count(); as count) {
      <div class="text-foreground/60 flex flex-row items-center gap-1">
        <app-board-group-header-seperator />
        <button
          class="hover:bg-primary/20 flex h-9 cursor-pointer appearance-none flex-row items-center gap-2 rounded-sm px-5 transition-[background-color,color] duration-140 ease-in-out outline-none"
          (click)="onClearClicked()"
          appTooltip="Clear Task Selection">
          <strong>{{ count + ' ' }}</strong>
          <span> tasks selected</span>
          <svg lucideListX size="20" class="close-btn"></svg>
        </button>

        @for (action of actions(); track action.label) {
          <app-board-group-header-action
            [label]="action.label"
            [icon]="action.icon"
            (action)="action.action()" />
        }
      </div>
    }
  `,
})
export class BoardGroupsSelectionComponent {
  private store = inject(Store);
  private dialog = inject(DialogService);

  selected = this.store.selectSignal(selectSelectedTasks);
  count = this.store.selectSignal(selectSelectedTasksCount);
  permissions = this.store.selectSignal(selectPermissions);

  actions = computed(() => {
    const actions = [];
    const permissions = this.permissions();

    if (permissions.has(netptunePermissions.tasks.delete)) {
      actions.push({
        label: 'Delete selected tasks',
        action: this.onDeleteClicked.bind(this),
        icon: LucideTrash2,
      });
    }
    if (permissions.has(netptunePermissions.tasks.move)) {
      actions.push({
        label: 'Move selected tasks to another group',
        action: this.onMoveTasksClicked.bind(this),
        icon: LucideMove,
      });
    }
    if (permissions.has(netptunePermissions.tasks.reassign)) {
      actions.push({
        label: 'Reassign tasks to user',
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
