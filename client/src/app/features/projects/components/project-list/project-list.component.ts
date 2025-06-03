import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import * as ProjectsActions from '@core/store/projects/projects.actions';
import * as ProjectsSelectors from '@core/store/projects/projects.selectors';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';

@Component({
    selector: 'app-project-list',
    templateUrl: './project-list.component.html',
    styleUrls: ['./project-list.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false
})
export class ProjectListComponent implements OnInit {
  projects$!: Observable<ProjectViewModel[]>;

  constructor(private store: Store) {}

  ngOnInit() {
    this.projects$ = this.store.select(ProjectsSelectors.selectAllProjects);
  }

  deleteClicked(project: ProjectViewModel) {
    this.store.dispatch(ProjectsActions.deleteProject({ project }));
  }

  trackById(_: number, project: ProjectViewModel) {
    return project.id;
  }
}
