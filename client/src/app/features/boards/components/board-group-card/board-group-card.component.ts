import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import * as actions from '@boards/store/groups/board-groups.actions';
import { Selected } from '@core/models/selected';
import { BoardViewTask } from '@core/models/view-models/board-view';
import { TaskDetailDialogComponent } from '@entry/dialogs/task-detail-dialog/task-detail-dialog.component';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-board-group-card',
  templateUrl: './board-group-card.component.html',
  styleUrls: ['./board-group-card.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardGroupCardComponent {
  @Input() task: Selected<BoardViewTask>;
  @Input() groupId: number;

  constructor(private dialog: MatDialog, private store: Store) {}

  onTaskClicked(event: KeyboardEvent | MouseEvent) {
    const id = this.task.id;
    const selected = this.task.selected;

    if (event.shiftKey) {
      const groupId = this.groupId;

      this.store.dispatch(
        selected
          ? actions.deSelectTaskBulk({ id, groupId })
          : actions.selectTaskBulk({ id, groupId })
      );
    } else if (event.ctrlKey) {
      this.store.dispatch(
        selected ? actions.deSelectTask({ id }) : actions.selectTask({ id })
      );
    } else {
      this.dialog.open(TaskDetailDialogComponent, {
        width: TaskDetailDialogComponent.width,
        data: this.task,
        panelClass: 'app-modal-class',
        autoFocus: false,
      });
    }
  }

  trackByTag(_: number, tag: string) {
    return tag;
  }
}
