import { Component, input } from '@angular/core';
import { projectDetailResource } from '@app/core/resources/project.resource';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { ProjectDetailComponent } from '@projects/components/project-detail/project-detail.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SkeletonComponent } from '@static/components/skeleton/skeleton.component';

@Component({
  selector: 'app-project-detail-view',
  imports: [
    ErrorStateComponent,
    PageContainerComponent,
    PageHeaderComponent,
    ProjectDetailComponent,
    SkeletonComponent,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header
        i18n-title="Page title for a single project"
        title="Project" />

      @if (project.isLoading()) {
        <div
          class="border-border bg-card overflow-hidden rounded-lg border shadow-sm"
          role="status"
          i18n-aria-label="Accessible label while a project loads"
          aria-label="Loading project">
          <div class="border-border flex items-center gap-3 border-b px-6 py-5">
            <app-skeleton class="h-9 w-9 shrink-0 rounded-lg" />
            <div class="min-w-0 flex-1">
              <app-skeleton class="h-4 w-40" />
              <app-skeleton class="mt-2 h-3 w-64" />
            </div>
          </div>

          <div class="grid max-w-2xl gap-6 px-6 py-5">
            @for (row of skeletonRows; track $index) {
              <div>
                <app-skeleton class="h-3 w-24" />
                <app-skeleton class="mt-2 h-10 w-full" />
              </div>
            }
          </div>

          <div class="border-border border-t px-6 py-4">
            <app-skeleton class="h-9 w-32" />
          </div>
        </div>
      } @else if (project.error()) {
        <app-error-state
          i18n-title="Shown when a project fails to load"
          title="This project could not be loaded"
          i18n-description="Advice shown when a page fails to load"
          description="Check your connection and try again."
          (retry)="project.reload()" />
      } @else {
        <div class="flex flex-col gap-6">
          <app-project-detail [project]="project.value()" />
        </div>
      }
    </app-page-container>
  `,
})
export class ProjectDetailViewComponent {
  id = input.required<string>();
  project = projectDetailResource(this.id);

  protected readonly skeletonRows = Array.from({ length: 4 });
}
