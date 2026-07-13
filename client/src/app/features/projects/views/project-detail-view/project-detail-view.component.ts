import { Component, input } from '@angular/core';
import { projectDetailResource } from '@app/core/resources/project.resource';
import { ProjectDetailComponent } from '@projects/components/project-detail/project-detail.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';

@Component({
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    PageLoadingComponent,
    ProjectDetailComponent,
  ],
  template: `<app-page-container
    [verticalPadding]="false"
    [fullHeight]="true"
    [centerPage]="true"
    [marginBottom]="true">
    <app-page-header title="Project" />

    @if (project.isLoading()) {
      <app-page-loading />
    } @else {
      <app-project-detail [project]="project.value()" />
    }
  </app-page-container> `,
})
export class ProjectDetailViewComponent {
  id = input.required<string>();
  project = projectDetailResource(this.id);
}
