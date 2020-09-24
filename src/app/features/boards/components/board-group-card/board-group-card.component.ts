import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import {
  deSelectTask,
  selectTask,
} from '@boards/store/groups/board-groups.actions';
import { Selected } from '@core/models/selected';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { TaskDetailDialogComponent } from '@entry/dialogs/task-detail-dialog/task-detail-dialog.component';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-board-group-card',
  templateUrl: './board-group-card.component.html',
  styleUrls: ['./board-group-card.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardGroupCardComponent {
  @Input() task: Selected<TaskViewModel>;

  constructor(private dialog: MatDialog, private store: Store) {}

  onTaskClicked(event: KeyboardEvent | MouseEvent) {
    const id = this.task.id;
    const selected = this.task.selected;

    if (event.shiftKey) {
      this.store.dispatch(selected ? deSelectTask({ id }) : selectTask({ id }));
    } else {
      this.dialog.open(TaskDetailDialogComponent, {
        width: '800px',
        data: this.task,
        panelClass: 'app-modal-class',
      });
    }
  }

  trackByTag(_: number, tag: string) {
    return tag;
  }
}
