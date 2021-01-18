import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { selectProjectDetail } from '@core/store/projects/projects.selectors';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-project-detail',
  templateUrl: './project-detail.component.html',
  styleUrls: ['./project-detail.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectDetailComponent implements OnInit {
  project$: Observable<ProjectViewModel>;

  constructor(private store: Store) {}

  ngOnInit() {
    this.project$ = this.store.select(selectProjectDetail);
  }
}
