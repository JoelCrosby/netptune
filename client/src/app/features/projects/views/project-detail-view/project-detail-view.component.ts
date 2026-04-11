import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import { selectProjectDetailLoading } from '@core/store/projects/projects.selectors';
import { Store } from '@ngrx/store';
import { ProjectDetailComponent } from '@projects/components/project-detail/project-detail.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';

@Component({
  templateUrl: './project-detail-view.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    SpinnerComponent,
    ProjectDetailComponent,
  ],
})
export class ProjectDetailViewComponent {
  private store = inject(Store);

  loading = this.store.selectSignal(selectProjectDetailLoading);
}
