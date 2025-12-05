import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { DialogService } from '@core/services/dialog.service';
import { loadProjects } from '@core/store/projects/projects.actions';
import { selectProjectsLoading } from '@core/store/projects/projects.selectors';
import { ProjectDialogComponent } from '@entry/dialogs/project-dialog/project-dialog.component';
import { Store } from '@ngrx/store';
import { ProjectListComponent } from '@projects/components/project-list/project-list.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';

@Component({
  templateUrl: './projects-view.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    MatProgressSpinner,
    ProjectListComponent,
  ],
})
export class ProjectsViewComponent {
  dialog = inject(DialogService);
  store = inject(Store);

  loading = this.store.selectSignal(selectProjectsLoading);

  constructor() {
    this.store.dispatch(loadProjects());
  }

  showAddModal() {
    this.dialog.open(ProjectDialogComponent, { width: '512px' });
  }
}
