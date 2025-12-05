import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  OnDestroy,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  FormBuilder,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { UpdateProjectRequest } from '@core/models/requests/upadte-project-request';
import {
  clearProjectDetail,
  updateProject,
} from '@core/store/projects/projects.actions';
import {
  selectProjectDetail,
  selectUpdateProjectLoading,
} from '@core/store/projects/projects.selectors';
import { Store } from '@ngrx/store';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';

@Component({
  selector: 'app-project-detail',
  templateUrl: './project-detail.component.html',
  styleUrls: ['./project-detail.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    FormInputComponent,
    FormTextAreaComponent,
    MatButton,
  ],
})
export class ProjectDetailComponent implements OnDestroy {
  private store = inject(Store);
  private fb = inject(FormBuilder);

  project = this.store.selectSignal(selectProjectDetail);
  loading = this.store.selectSignal(selectUpdateProjectLoading);

  form = this.fb.nonNullable.group(
    {
      id: [null as number | null, [Validators.required]],
      key: ['', [Validators.required, Validators.maxLength(6)]],
      name: ['', [Validators.required, Validators.maxLength(128)]],
      description: ['', [Validators.maxLength(4096)]],
      repositoryUrl: ['', [Validators.maxLength(1024)]],
    },
    { updateOn: 'change' }
  );

  valueChanges = toSignal(this.form.valueChanges);
  updateDisabled = computed(() => {
    return (
      this.valueChanges() ||
      this.form.pristine ||
      this.form.invalid ||
      this.loading()
    );
  });

  constructor() {
    effect(() => {
      const project = this.project();

      if (!project) return;

      this.form.setValue({
        id: project.id,
        key: project.key,
        name: project.name,
        description: project.description,
        repositoryUrl: project.repositoryUrl,
      });
    });

    effect(() => {
      if (this.loading() && this.form.enabled) {
        this.form.disable();
      } else if (this.form.disabled) {
        this.form.enable();
      }
    });
  }

  ngOnDestroy() {
    this.store.dispatch(clearProjectDetail());
  }

  updateClicked() {
    if (this.form.invalid) {
      return;
    }

    this.form.markAsPristine();

    const { id, name, description, repositoryUrl, key } = this.form.controls;

    if (id.value === null) return;

    const project: UpdateProjectRequest = {
      id: id.value,
      name: name.value,
      description: description.value,
      repositoryUrl: repositoryUrl.value,
      key: key.value,
    };

    this.store.dispatch(updateProject({ project }));
  }
}
