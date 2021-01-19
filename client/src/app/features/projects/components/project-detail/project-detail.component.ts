import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup } from '@angular/forms';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { selectProjectDetail } from '@core/store/projects/projects.selectors';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

@Component({
  selector: 'app-project-detail',
  templateUrl: './project-detail.component.html',
  styleUrls: ['./project-detail.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectDetailComponent implements OnInit {
  project$: Observable<ProjectViewModel>;

  formGroup: FormGroup;

  constructor(private store: Store, private fb: FormBuilder) {}

  ngOnInit() {
    this.project$ = this.store.select(selectProjectDetail).pipe(
      tap((project) => {
        this.formGroup = this.fb.group({
          key: [project.key],
          name: [project.name],
          description: [project.description],
          repositoryUrl: [project.repositoryUrl],
          defaultBoard: [project.defaultBoardIdentifier],
        });
      })
    );
  }
}
