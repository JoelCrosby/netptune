import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { MatTooltip } from '@angular/material/tooltip';
import {
  clearTaskSelection,
  deleteSelectedTasks,
} from '@boards/store/groups/board-groups.actions';
import {
  selectSelectedTasks,
  selectSelectedTasksCount,
} from '@boards/store/groups/board-groups.selectors';
import { DialogService } from '@core/services/dialog.service';
import { Store } from '@ngrx/store';
import { MoveTasksDialogComponent } from '../move-tasks-dialog/move-tasks-dialog.component';
import { ReassignTasksDialogComponent } from '../reassign-tasks-dialog/reassign-tasks-dialog.component';

@Component({
  selector: 'app-board-groups-selection',
  templateUrl: './board-groups-selection.component.html',
  styleUrls: ['./board-groups-selection.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIcon, MatTooltip],
})
export class BoardGroupsSelectionComponent {
  private store = inject(Store);
  private dialog = inject(DialogService);

  selected = this.store.selectSignal(selectSelectedTasks);
  count = this.store.selectSignal(selectSelectedTasksCount);

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
