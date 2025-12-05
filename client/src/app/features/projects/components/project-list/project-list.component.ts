import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { deleteProject } from '@core/store/projects/projects.actions';
import { selectAllProjects } from '@core/store/projects/projects.selectors';
import { Store } from '@ngrx/store';
import { CardListComponent } from '@static/components/card-list/card-list.component';
import { ProjectListItemComponent } from '../project-list-item/project-list-item.component';

@Component({
  selector: 'app-project-list',
  templateUrl: './project-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CardListComponent, ProjectListItemComponent],
})
export class ProjectListComponent {
  private store = inject(Store);

  projects = this.store.selectSignal(selectAllProjects);

  deleteClicked(project: ProjectViewModel) {
    this.store.dispatch(deleteProject({ project }));
  }
}
