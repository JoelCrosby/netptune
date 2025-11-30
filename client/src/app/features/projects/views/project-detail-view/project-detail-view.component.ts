import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  inject,
} from '@angular/core';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { ActivatedRoute } from '@angular/router';
import { loadProjectDetail } from '@core/store/projects/projects.actions';
import { selectProjectDetailLoading } from '@core/store/projects/projects.selectors';
import { Store } from '@ngrx/store';
import { ProjectDetailComponent } from '@projects/components/project-detail/project-detail.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { first } from 'rxjs/operators';

@Component({
  templateUrl: './project-detail-view.component.html',
  styleUrls: ['./project-detail-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    MatProgressSpinner,
    ProjectDetailComponent,
  ],
})
export class ProjectDetailViewComponent implements AfterViewInit {
  private store = inject(Store);
  private route = inject(ActivatedRoute);

  loading = this.store.selectSignal(selectProjectDetailLoading);

  ngAfterViewInit() {
    this.route.paramMap.pipe(first()).subscribe((params) => {
      const projectKey = params.get('id');

      if (!projectKey) return;

      this.store.dispatch(loadProjectDetail({ projectKey }));
    });
  }
}
