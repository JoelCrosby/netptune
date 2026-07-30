import { Component, input } from '@angular/core';
import { projectDetailResource } from '@app/core/resources/project.resource';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { ProjectDetailComponent } from '@projects/components/project-detail/project-detail.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';

@Component({
  imports: [
    ErrorStateComponent,
    PageContainerComponent,
    PageHeaderComponent,
    PageLoadingComponent,
    ProjectDetailComponent,
  ],
  template: `
    <app-page-container
      [verticalPadding]="false"
      [fullHeight]="true"
      [centerPage]="true"
      [marginBottom]="true">
      <app-page-header
        i18n-title="Page title for a single project"
        title="Project" />

      @if (project.isLoading()) {
        <app-page-loading />
      } @else if (project.error()) {
        <app-error-state
          i18n-title="Shown when a project fails to load"
          title="This project could not be loaded"
          i18n-description="Advice shown when a page fails to load"
          description="Check your connection and try again."
          (retry)="project.reload()" />
      } @else {
        <app-project-detail [project]="project.value()" />
      }
    </app-page-container>
  `,
})
export class ProjectDetailViewComponent {
  id = input.required<string>();
  project = projectDetailResource(this.id);
}
