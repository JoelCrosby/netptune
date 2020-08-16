import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  OnInit,
} from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { ProjectViewModel } from '@app/core/models/view-models/project-view-model';
import { TextHelpers } from '@app/core/util/text-helpers';
import { ConfirmDialogComponent } from '@app/entry/dialogs/confirm-dialog/confirm-dialog.component';
import * as ProjectsActions from '@core/store/projects/projects.actions';
import * as ProjectsSelectors from '@core/store/projects/projects.selectors';
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

  constructor(private store: Store, private dialog: MatDialog) {}

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
    this.store.dispatch(ProjectsActions.deleteProject({ project }));
  }

  trackById(index: number, project: ProjectViewModel) {
    return project.id;
  }
}
