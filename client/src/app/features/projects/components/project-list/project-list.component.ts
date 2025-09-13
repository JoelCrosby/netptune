import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import * as ProjectsActions from '@core/store/projects/projects.actions';
import * as ProjectsSelectors from '@core/store/projects/projects.selectors';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { CardListComponent } from '@static/components/card-list/card-list.component';
import { AsyncPipe } from '@angular/common';
import { ProjectListItemComponent } from '../project-list-item/project-list-item.component';

@Component({
  selector: 'app-project-list',
  templateUrl: './project-list.component.html',
  styleUrls: ['./project-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CardListComponent, ProjectListItemComponent, AsyncPipe],
})
export class ProjectListComponent implements OnInit {
  private store = inject(Store);

  projects$!: Observable<ProjectViewModel[]>;

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
