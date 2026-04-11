import {
  ChangeDetectionStrategy,
  Component,
  inject,
  OnDestroy,
  signal,
} from '@angular/core';
import {
  disabled,
  form,
  FormField,
  maxLength,
  required,
} from '@angular/forms/signals';
import { FlatButtonComponent } from '@app/static/components/button/flat-button.component';
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
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormInputComponent,
    FormTextAreaComponent,
    FlatButtonComponent,
    FormField,
  ],
  template: `@if (project(); as project) {
    <div>
      <form class="w-full max-w-[512px]" (submit)="updateClicked($event)">
        <app-form-input
          [formField]="projectForm.name"
          label="Name"
          maxLength="1024">
        </app-form-input>
        <app-form-textarea
          [formField]="projectForm.description"
          label="Description"
          rows="6">
        </app-form-textarea>
        <div class="border-border my-8 border-b-2"></div>
        <div class="flex items-center">
          <app-form-input
            [formField]="projectForm.key"
            label="Project ID"
            class="w-[120px]"
            maxLength="6">
          </app-form-input>
          <div>
            <small class="block px-[1.4rem] opacity-60">
              The Project ID is displayed as the first part of task's ID
            </small>
            <small class="block px-[1.4rem] opacity-60">
              max 6 characters. should be unique to workspace
            </small>
          </div>
        </div>
        <app-form-input
          [formField]="projectForm.repositoryUrl"
          label="Repository URL"
          maxLength="1024">
        </app-form-input>
        <button
          app-flat-button
          color="primary"
          [disabled]="projectForm().disabled()">
          Save Changes
        </button>
      </form>
    </div>
  } `,
})
export class ProjectDetailComponent implements OnDestroy {
  private store = inject(Store);

  project = this.store.selectSignal(selectProjectDetail);
  loading = this.store.selectSignal(selectUpdateProjectLoading);

  projectFormModel = signal({
    id: this.project()?.id ?? (null as number | null),
    key: this.project()?.key ?? '',
    name: this.project()?.name ?? '',
    description: this.project()?.description ?? '',
    repositoryUrl: this.project()?.repositoryUrl ?? '',
  });

  projectForm = form(this.projectFormModel, (schema) => {
    required(schema.id);
    required(schema.key);
    required(schema.name);
    maxLength(schema.key, 6);
    maxLength(schema.name, 128);
    maxLength(schema.description, 4096);
    maxLength(schema.repositoryUrl, 1024);
    disabled(schema, () => this.loading());
  });

  ngOnDestroy() {
    this.store.dispatch(clearProjectDetail());
  }

  updateClicked(event: Event) {
    event.preventDefault();

    if (this.projectForm().invalid()) {
      return;
    }

    this.projectForm().reset();

    const { name, description, repositoryUrl, key } = this.projectForm;
    const id = this.projectForm.id().value();

    if (id === null || id === undefined) return;

    const project: UpdateProjectRequest = {
      id,
      name: name().value(),
      description: description().value(),
      repositoryUrl: repositoryUrl().value(),
      key: key().value(),
    };

    this.store.dispatch(updateProject({ project }));
  }
}
