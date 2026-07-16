import { Component, computed, inject } from '@angular/core';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { DialogService } from '@core/services/dialog.service';
import { loadProjects } from '@core/store/projects/projects.actions';
import {
  selectAllProjects,
  selectProjectsLoading,
} from '@core/store/projects/projects.selectors';
import { dispatchForWorkspace } from '@core/util/dispatch-for-workspace';
import { ProjectDialogComponent } from '@entry/dialogs/project-dialog/project-dialog.component';
import { Store } from '@ngrx/store';
import { ProjectListComponent } from '@projects/components/project-list/project-list.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { netptunePermissions } from '@app/core/auth/permissions';
import { selectHasPermission } from '@app/core/store/auth/auth.selectors';
import { LucideFolderOpen, LucidePlus } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';

@Component({
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    PageLoadingComponent,
    ProjectListComponent,
    EmptyStateComponent,
    FlatButtonComponent,
    LucideFolderOpen,
    LucidePlus,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      @if (canCreateProjects()) {
        <app-page-header
          title="Projects"
          actionTitle="Create Project"
          [count]="count()"
          (actionClick)="showAddModal()" />
      } @else {
        <app-page-header title="Projects" [count]="count()" />
      }

      @if (loading()) {
        <app-page-loading />
      } @else if (projects().length === 0) {
        <app-empty-state
          title="There are currently no projects."
          description="Create your first project to organise related boards and tasks.">
          <svg emptyStateIcon size="38" lucideFolderOpen></svg>

          @if (canCreateProjects()) {
            <button
              emptyStateAction
              app-flat-button
              type="button"
              (click)="showAddModal()">
              <svg size="20" lucidePlus></svg>
              <span>Create Project</span>
            </button>
          }
        </app-empty-state>
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
  projects = this.store.selectSignal(selectAllProjects);
  count = computed(() => (this.loading() ? null : this.projects().length));
  canCreateProjects = this.store.selectSignal(
    selectHasPermission(netptunePermissions.projects.create)
  );

  constructor() {
    dispatchForWorkspace(() => loadProjects.init());
  }

  showAddModal() {
    this.dialog.open(ProjectDialogComponent, { width: '512px' });
  }
}
