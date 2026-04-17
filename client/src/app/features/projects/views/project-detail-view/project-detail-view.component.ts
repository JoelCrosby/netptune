import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { selectProjectDetailLoading } from '@core/store/projects/projects.selectors';
import { Store } from '@ngrx/store';
import { ProjectDetailComponent } from '@projects/components/project-detail/project-detail.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    SpinnerComponent,
    ProjectDetailComponent,
  ],
  template: `<app-page-container
    [verticalPadding]="false"
    [fullHeight]="true"
    [centerPage]="true"
    [marginBottom]="true">
    <app-page-header title="Project" />

    @if (loading()) {
      <div class="flex h-full flex-col items-center justify-center">
        <app-spinner diameter="32px" />
      </div>
    } @else {
      <app-project-detail />
    }
  </app-page-container> `,
})
export class ProjectDetailViewComponent {
  private store = inject(Store);

  loading = this.store.selectSignal(selectProjectDetailLoading);
}
