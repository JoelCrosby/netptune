import {
  Component,
  inject,
  input,
  linkedSignal,
  OnDestroy,
} from '@angular/core';
import {
  disabled,
  form,
  FormField,
  maxLength,
  required,
} from '@angular/forms/signals';
import { ProjectViewModel } from '@app/core/models/view-models/project-view-model';
import { FlatButtonComponent } from '@app/static/components/button/flat-button.component';
import { UpdateProjectRequest } from '@core/models/requests/upadte-project-request';
import { statusResource } from '@core/resources/status.resources';
import {
  clearProjectDetail,
  updateProject,
} from '@core/store/projects/projects.actions';
import { selectUpdateProjectLoading } from '@core/store/projects/projects.selectors';
import { Store } from '@ngrx/store';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';

@Component({
  selector: 'app-project-detail',
  imports: [
    FormInputComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    FormTextAreaComponent,
    FlatButtonComponent,
    FormField,
  ],
  template: `
    <div>
      <form class="w-full max-w-lg" (submit)="updateClicked($event)">
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
            class="w-30"
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

        @if (statuses.value()) {
          <app-form-select
            [formField]="projectForm.defaultStatusId"
            label="Default task status">
            @for (status of statuses.value(); track status.id) {
              <app-form-select-option [value]="status.id">
                {{ status.name }}
              </app-form-select-option>
            }
          </app-form-select>
        }

        <button
          app-flat-button
          color="primary"
          [disabled]="projectForm().disabled()">
          Save Changes
        </button>
      </form>
    </div>
  `,
})
export class ProjectDetailComponent implements OnDestroy {
  private store = inject(Store);

  project = input<ProjectViewModel>();
  loading = this.store.selectSignal(selectUpdateProjectLoading);
  statuses = statusResource();

  projectFormModel = linkedSignal(() => ({
    id: this.project()?.id ?? (null as number | null),
    key: this.project()?.key ?? '',
    name: this.project()?.name ?? '',
    description: this.project()?.description ?? '',
    repositoryUrl: this.project()?.repositoryUrl ?? '',
    defaultStatusId: this.project()?.defaultStatusId ?? 0,
  }));

  projectForm = form(this.projectFormModel, (schema) => {
    required(schema.id);
    required(schema.key);
    required(schema.name);
    required(schema.defaultStatusId);
    maxLength(schema.key, 6);
    maxLength(schema.name, 128);
    maxLength(schema.description, 4096);
    maxLength(schema.repositoryUrl, 1024);
    disabled(schema, { when: () => this.loading() });
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

    const { name, description, repositoryUrl, key, defaultStatusId } =
      this.projectForm;
    const id = this.projectForm.id().value();

    if (id === null || id === undefined) return;

    const project: UpdateProjectRequest = {
      id,
      name: name().value(),
      description: description().value(),
      repositoryUrl: repositoryUrl().value(),
      key: key().value(),
      defaultStatusId: defaultStatusId().value() || null,
    };

    this.store.dispatch(updateProject.init({ project }));
  }
}
