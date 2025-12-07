import {
  ChangeDetectionStrategy,
  Component,
  inject,
  OnDestroy,
  signal,
} from '@angular/core';
import {
  disabled,
  Field,
  form,
  maxLength,
  required,
} from '@angular/forms/signals';
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
  imports: [FormInputComponent, FormTextAreaComponent, MatButton, Field],
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
