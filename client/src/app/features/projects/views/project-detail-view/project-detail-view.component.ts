import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
} from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AppState } from '@core/core.state';
import { loadProjectDetail } from '@core/store/projects/projects.actions';
import { selectProjectDetailLoading } from '@core/store/projects/projects.selectors';
import { Store } from '@ngrx/store';
import { first } from 'rxjs/operators';

@Component({
  templateUrl: './project-detail-view.component.html',
  styleUrls: ['./project-detail-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectDetailViewComponent implements AfterViewInit {
  loading$ = this.store.select(selectProjectDetailLoading);

  constructor(private store: Store<AppState>, private route: ActivatedRoute) {}

  ngAfterViewInit() {
    this.route.paramMap.pipe(first()).subscribe((params) => {
      const projectKey = params.get('id');

      if (!projectKey) return;

      this.store.dispatch(loadProjectDetail({ projectKey }));
    });
  }
}
