import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import * as selectors from '@boards/store/groups/board-groups.selectors';
import * as actions from '@boards/store/groups/board-groups.actions';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { MatDialog } from '@angular/material/dialog';
import { MoveTasksDialogComponent } from '../move-tasks-dialog/move-tasks-dialog.component';
import { ReassignTasksDialogComponent } from '../reassign-tasks-dialog/reassign-tasks-dialog.component';
import { AppState } from '@core/core.state';

@Component({
  selector: 'app-board-groups-selection',
  templateUrl: './board-groups-selection.component.html',
  styleUrls: ['./board-groups-selection.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardGroupsSelectionComponent implements OnInit {
  selected$!: Observable<number[]>;
  count$!: Observable<number>;

  constructor(private store: Store<AppState>, private dialog: MatDialog) {}

  ngOnInit() {
    this.selected$ = this.store.select(selectors.selectSelectedTasks);
    this.count$ = this.store.select(selectors.selectSelectedTasksCount);
  }

  onClearClicked() {
    this.store.dispatch(actions.clearTaskSelection());
  }

  onDeleteClicked() {
    this.store.dispatch(actions.deleteSelectedTasks());
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
