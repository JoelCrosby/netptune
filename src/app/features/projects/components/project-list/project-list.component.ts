import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  OnInit,
} from '@angular/core';
import { ProjectViewModel } from '@app/core/models/view-models/project-view-model';
import * as ProjectsActions from '@core/store/projects/projects.actions';
import * as ProjectsSelectors from '@core/store/projects/projects.selectors';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { startWith } from 'rxjs/operators';

@Component({
  selector: 'app-project-list',
  templateUrl: './project-list.component.html',
  styleUrls: ['./project-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectListComponent implements OnInit, AfterViewInit {
  projects$: Observable<ProjectViewModel[]>;
  loading$: Observable<boolean>;

  constructor(private store: Store) {}

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

  trackById(_: number, project: ProjectViewModel) {
    return project.id;
  }
}
