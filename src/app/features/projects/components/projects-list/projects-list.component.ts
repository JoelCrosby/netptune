import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  OnInit,
} from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { AppState } from '@app/core/core.state';
import { Project } from '@app/core/models/project';
import { ProjectViewModel } from '@app/core/models/view-models/project-view-model';
import * as ProjectsActions from '@core/store/projects/projects.actions';
import * as ProjectsSelectors from '@core/store/projects/projects.selectors';
import { TextHelpers } from '@app/core/util/text-helpers';
import { ConfirmDialogComponent } from '@app/entry/dialogs/confirm-dialog/confirm-dialog.component';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { startWith } from 'rxjs/operators';

@Component({
  selector: 'app-projects-list',
  templateUrl: './projects-list.component.html',
  styleUrls: ['./projects-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectsListComponent implements OnInit, AfterViewInit {
  projects$: Observable<ProjectViewModel[]>;
  loading$: Observable<boolean>;

  constructor(private store: Store<AppState>, private dialog: MatDialog) {}

  ngOnInit() {
    this.projects$ = this.store.select(ProjectsSelectors.selectAllProjects);
    this.loading$ = this.store
      .select(ProjectsSelectors.selectProjectsLoading)
      .pipe(startWith(true));
  }

  ngAfterViewInit() {
    this.store.dispatch(ProjectsActions.loadProjects());
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

  trackById(index: number, project: Project) {
    return project.id;
  }
}
