import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { MatDialog } from '@angular/material/dialog';
import { TaskDetailDialogComponent } from '@entry/dialogs/task-detail-dialog/task-detail-dialog.component';

@Component({
  selector: 'app-board-group-card',
  templateUrl: './board-group-card.component.html',
  styleUrls: ['./board-group-card.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardGroupCardComponent {
  @Input() task: TaskViewModel;

  constructor(private dialog: MatDialog) {}

  onTaskClicked() {
    this.dialog.open(TaskDetailDialogComponent, {
      width: '800px',
      data: this.task,
      panelClass: 'app-modal-class',
    });
  }

  trackByTag(_: number, tag: string) {
    return tag;
  }
}
