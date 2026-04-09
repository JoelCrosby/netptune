import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { deleteProject } from '@core/store/projects/projects.actions';
import { selectAllProjects } from '@core/store/projects/projects.selectors';
import { Store } from '@ngrx/store';
import { CardListComponent } from '@app/static/components/card/card-list.component';
import { ProjectListItemComponent } from '../project-list-item/project-list-item.component';

@Component({
  selector: 'app-project-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CardListComponent, ProjectListItemComponent],
  template: `
    <app-card-list>
      @for (project of projects(); track project.id) {
        <app-project-list-item [project]="project" />
      }
    </app-card-list>
  `,
})
export class ProjectListComponent {
  private store = inject(Store);

  projects = this.store.selectSignal(selectAllProjects);

  deleteClicked(project: ProjectViewModel) {
    this.store.dispatch(deleteProject({ project }));
  }
}
