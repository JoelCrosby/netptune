import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
} from '@angular/core';
import {
  FormControl,
  FormGroup,
  Validators,
  FormsModule,
  ReactiveFormsModule,
} from '@angular/forms';
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
import { NgIf, AsyncPipe } from '@angular/common';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';
import { MatButton } from '@angular/material/button';

@Component({
  selector: 'app-project-detail',
  templateUrl: './project-detail.component.html',
  styleUrls: ['./project-detail.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    NgIf,
    FormsModule,
    ReactiveFormsModule,
    FormInputComponent,
    FormTextAreaComponent,
    MatButton,
    AsyncPipe,
  ],
})
export class ProjectDetailComponent implements OnInit, OnDestroy {
  project$!: Observable<ProjectViewModel | null | undefined>;
  boards$!: Observable<BoardViewModel[]>;
  updateDisabled$!: Observable<boolean>;
  onDestroy$ = new Subject<void>();

  formGroup = new FormGroup(
    {
      id: new FormControl<number | null>(null, [Validators.required]),
      key: new FormControl('', [Validators.required, Validators.maxLength(6)]),
      name: new FormControl('', [
        Validators.required,
        Validators.maxLength(128),
      ]),
      description: new FormControl('', [Validators.maxLength(4096)]),
      repositoryUrl: new FormControl('', [Validators.maxLength(1024)]),
    },
    { updateOn: 'change' }
  );

  get id() {
    return this.formGroup.controls.id;
  }

  get name() {
    return this.formGroup.controls.name;
  }

  get description() {
    return this.formGroup.controls.description;
  }

  get repositoryUrl() {
    return this.formGroup.controls.repositoryUrl;
  }

  get key() {
    return this.formGroup.controls.key;
  }

  constructor(private store: Store) {}

  ngOnInit() {
    this.project$ = this.store.select(selectProjectDetail).pipe(
      tap((project) => {
        if (!project) return;

        this.formGroup.setValue({
          id: project.id,
          key: project.key,
          name: project.name,
          description: project.description,
          repositoryUrl: project.repositoryUrl,
        });

        this.updateDisabled$ = combineLatest([
          this.formGroup.valueChanges,
          this.store.select(selectUpdateProjectLoading),
        ]).pipe(
          takeUntil(this.onDestroy$),
          map(
            ([, loading]) =>
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
      id: this.id.value as number,
      name: this.name.value as string,
      description: this.description.value as string,
      repositoryUrl: this.repositoryUrl.value as string,
      key: this.key.value as string,
    };

    this.store.dispatch(updateProject({ project }));
  }
}
