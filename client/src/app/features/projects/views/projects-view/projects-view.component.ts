import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { ProjectDialogComponent } from '@entry/dialogs/project-dialog/project-dialog.component';

@Component({
  templateUrl: './projects-view.component.html',
  styleUrls: ['./projects-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectsViewComponent {
  constructor(public dialog: MatDialog) {}

  showAddModal() {
    this.dialog.open(ProjectDialogComponent);
  }
}
