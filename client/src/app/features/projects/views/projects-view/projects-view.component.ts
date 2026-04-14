import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import { DialogService } from '@core/services/dialog.service';
import { loadProjects } from '@core/store/projects/projects.actions';
import { selectProjectsLoading } from '@core/store/projects/projects.selectors';
import { ProjectDialogComponent } from '@entry/dialogs/project-dialog/project-dialog.component';
import { Store } from '@ngrx/store';
import { ProjectListComponent } from '@projects/components/project-list/project-list.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { netptunePermissions } from '@app/core/auth/permissions';
import { selectHasPermission } from '@app/core/auth/store/auth.selectors';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    SpinnerComponent,
    ProjectListComponent,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      @if (canCreateProjects()) {
        <app-page-header
          title="Projects"
          actionTitle="Create Project"
          (actionClick)="showAddModal()" />
      } @else {
        <app-page-header title="Projects" />
      }

      @if (loading()) {
        <div class="flex h-full flex-col items-center justify-center">
          <app-spinner diameter="32px" />
        </div>
      } @else {
        <app-project-list />
      }
    </app-page-container>
  `,
})
export class ProjectsViewComponent {
  dialog = inject(DialogService);
  store = inject(Store);

  loading = this.store.selectSignal(selectProjectsLoading);
  canCreateProjects = this.store.selectSignal(
    selectHasPermission(netptunePermissions.projects.create)
  );

  constructor() {
    this.store.dispatch(loadProjects());
  }

  showAddModal() {
    this.dialog.open(ProjectDialogComponent, { width: '512px' });
  }
}
