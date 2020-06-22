import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { selectAllProjects } from '@app/core/projects/projects.selectors';
import { dropIn } from '@core/animations/animations';
import { ProjectDialogComponent } from '@entry/dialogs/project-dialog/project-dialog.component';

@Component({
  templateUrl: './projects-view.component.html',
  styleUrls: ['./projects-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  animations: [dropIn],
})
export class ProjectsViewComponent {
  constructor(public snackBar: MatSnackBar, public dialog: MatDialog) {}

  showAddModal() {
    this.dialog.open(ProjectDialogComponent);
  }
}
