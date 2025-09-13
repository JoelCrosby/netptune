import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
} from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { loadProjectDetail } from '@core/store/projects/projects.actions';
import { selectProjectDetailLoading } from '@core/store/projects/projects.selectors';
import { Store } from '@ngrx/store';
import { first } from 'rxjs/operators';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { NgIf, AsyncPipe } from '@angular/common';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { ProjectDetailComponent } from '@projects/components/project-detail/project-detail.component';

@Component({
  templateUrl: './project-detail-view.component.html',
  styleUrls: ['./project-detail-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    NgIf,
    MatProgressSpinner,
    ProjectDetailComponent,
    AsyncPipe,
  ],
})
export class ProjectDetailViewComponent implements AfterViewInit {
  loading$ = this.store.select(selectProjectDetailLoading);

  constructor(
    private store: Store,
    private route: ActivatedRoute
  ) {}

  ngAfterViewInit() {
    this.route.paramMap.pipe(first()).subscribe((params) => {
      const projectKey = params.get('id');

      if (!projectKey) return;

      this.store.dispatch(loadProjectDetail({ projectKey }));
    });
  }
}
