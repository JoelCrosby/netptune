import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
} from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { UpdateProjectRequest } from '@core/models/requests/upadte-project-request';
import { BoardViewModel } from '@core/models/view-models/board-view-model';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { updateProject } from '@core/store/projects/projects.actions';
import {
  selectProjectDetail,
  selectUpdateProjectLoading,
} from '@core/store/projects/projects.selectors';
import { Store } from '@ngrx/store';
import { combineLatest, Observable, Subject } from 'rxjs';
import { map, startWith, takeUntil, tap } from 'rxjs/operators';

@Component({
  selector: 'app-project-detail',
  templateUrl: './project-detail.component.html',
  styleUrls: ['./project-detail.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectDetailComponent implements OnInit, OnDestroy {
  project$: Observable<ProjectViewModel>;
  boards$: Observable<BoardViewModel[]>;
  updateDisabled$: Observable<boolean>;
  onDestroy$ = new Subject();

  formGroup: FormGroup;

  get id() {
    return this.formGroup.get('id');
  }

  get name() {
    return this.formGroup.get('name');
  }

  get description() {
    return this.formGroup.get('description');
  }

  get repositoryUrl() {
    return this.formGroup.get('repositoryUrl');
  }

  get key() {
    return this.formGroup.get('key');
  }

  constructor(private store: Store, private fb: FormBuilder) {}

  ngOnInit() {
    this.project$ = this.store.select(selectProjectDetail).pipe(
      tap((project) => {
        this.formGroup = this.fb.group(
          {
            id: [project.id, Validators.required],
            key: [project.key, [Validators.required, Validators.maxLength(6)]],
            name: [
              project.name,
              [Validators.required, Validators.maxLength(128)],
            ],
            description: [project.description, Validators.maxLength(4096)],
            repositoryUrl: [project.repositoryUrl, Validators.maxLength(1024)],
          },
          { updateOn: 'change' }
        );

        this.updateDisabled$ = combineLatest([
          this.formGroup.valueChanges,
          this.store.select(selectUpdateProjectLoading),
        ]).pipe(
          takeUntil(this.onDestroy$),
          map(
            ([_, loading]) =>
              this.formGroup.pristine || this.formGroup.invalid || loading
          ),
          startWith(true)
        );

        this.store
          .select(selectUpdateProjectLoading)
          .pipe(takeUntil(this.onDestroy$))
          .subscribe({
            next: (loading) => {
              if (loading && this.formGroup.enabled) {
                this.formGroup.disable();
              } else if (this.formGroup.disabled) {
                this.formGroup.enable();
              }
            },
          });
      })
    );
  }

  ngOnDestroy() {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  updateClicked() {
    if (this.formGroup.invalid) {
      return;
    }

    this.formGroup.markAsPristine();

    const project: UpdateProjectRequest = {
      id: this.id.value,
      name: this.name.value,
      description: this.description.value,
      repositoryUrl: this.repositoryUrl.value,
      key: this.key.value,
    };

    this.store.dispatch(updateProject({ project }));
  }
}
