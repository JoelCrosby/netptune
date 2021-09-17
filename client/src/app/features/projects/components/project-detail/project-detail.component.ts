import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
} from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  Validators,
  FormControl,
} from '@angular/forms';
import { AppState } from '@core/core.state';
import { UpdateProjectRequest } from '@core/models/requests/upadte-project-request';
import { BoardViewModel } from '@core/models/view-models/board-view-model';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import {
  clearProjectDetail,
  updateProject,
} from '@core/store/projects/projects.actions';
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
  project$!: Observable<ProjectViewModel | null | undefined>;
  boards$!: Observable<BoardViewModel[]>;
  updateDisabled$!: Observable<boolean>;
  onDestroy$ = new Subject();

  formGroup!: FormGroup;

  get id() {
    return this.formGroup.get('id') as FormControl;
  }

  get name() {
    return this.formGroup.get('name') as FormControl;
  }

  get description() {
    return this.formGroup.get('description') as FormControl;
  }

  get repositoryUrl() {
    return this.formGroup.get('repositoryUrl') as FormControl;
  }

  get key() {
    return this.formGroup.get('key') as FormControl;
  }

  constructor(private store: Store<AppState>, private fb: FormBuilder) {}

  ngOnInit() {
    this.project$ = this.store.select(selectProjectDetail).pipe(
      tap((project) => {
        if (!project) return;

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

    this.store.dispatch(clearProjectDetail());
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
