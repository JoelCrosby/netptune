import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { dropIn } from '@core/animations/animations';
import { AppState } from '@core/core.state';
import { Project } from '@core/models/project';
import { Store } from '@ngrx/store';
import { ProjectDialogComponent } from '@entry/dialogs/project-dialog/project-dialog.component';
import { ConfirmDialogComponent } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { TextHelpers } from '@app/core/util/text-helpers';
import { selectAllProjects } from '@app/core/projects/projects.selectors';
import * as ProjectsActions from '@app/core/projects/projects.actions';

@Component({
  selector: 'app-projects',
  templateUrl: './projects.index.component.html',
  styleUrls: ['./projects.index.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  animations: [dropIn],
})
export class ProjectsComponent implements OnInit {
  projects$ = this.store.select(selectAllProjects);

  constructor(
    public snackBar: MatSnackBar,
    public dialog: MatDialog,
    private store: Store<AppState>
  ) {}

  ngOnInit() {
    this.store.dispatch(ProjectsActions.loadProjects());
  }

  trackById(index: number, project: Project) {
    return project.id;
  }

  showAddModal() {
    this.dialog.open(ProjectDialogComponent);
  }

  deleteClicked(project: ProjectViewModel) {
    this.dialog
      .open(ConfirmDialogComponent, {
        width: '600px',
        data: {
          title: 'Are you sure you want to delete this project?',
          content: `${TextHelpers.truncate(project.name)}`,
          confirm: 'Delete Project',
        },
      })
      .afterClosed()
      .subscribe((result) => {
        if (result) {
          this.store.dispatch(ProjectsActions.deleteProject({ project }));
        }
      });
  }
}
