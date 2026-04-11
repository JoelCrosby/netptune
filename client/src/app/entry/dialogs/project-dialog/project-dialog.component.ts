import { DialogRef } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { FormField, form, minLength, required } from '@angular/forms/signals';
import { ButtonComponent } from '@static/components/button/button.component';
import { AddProjectRequest } from '@core/models/project';
import { createProject } from '@core/store/projects/projects.actions';
import { selectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';

@Component({
  selector: 'app-project-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormField,
    FormInputComponent,
    FormTextAreaComponent,
    DialogActionsDirective,
    ButtonComponent,
  ],
  template: `
    <h1 mat-dialog-title>Create Project</h1>

    <form app-dialog-content>
      <app-form-input
        [formField]="projectForm.name"
        label="Name"
        maxLength="1024">
      </app-form-input>

      <app-form-input
        [formField]="projectForm.repositoryUrl"
        label="Repository URl"
        maxLength="1024">
      </app-form-input>

      <app-form-textarea
        [formField]="projectForm.description"
        label="Description"
        maxLength="4096">
      </app-form-textarea>
    </form>

    <div app-dialog-actions align="end">
      <app-button variant="outlined" (click)="close()">Close</app-button>
      <app-button variant="filled" (click)="getResult()"
        >Create Project</app-button
      >
    </div>
  `,
})
export class ProjectDialogComponent {
  private store = inject(Store);
  dialogRef = inject<DialogRef<ProjectDialogComponent>>(DialogRef);

  currentWorkspace = this.store.selectSignal(selectCurrentWorkspace);

  prjectFormModel = signal({
    name: '',
    repositoryUrl: '',
    description: '',
    workspace: '',
    color: '',
  });

  projectForm = form(this.prjectFormModel, (schema) => {
    required(schema.name);
    minLength(schema.name, 4);
  });

  close() {
    this.dialogRef.close();
  }

  getResult() {
    const workspace = this.currentWorkspace();

    if (this.projectForm().invalid()) {
      return;
    }

    const { name, repositoryUrl, description, color } = this.projectForm;

    if (!workspace?.slug) return;

    const project: AddProjectRequest = {
      name: name().value(),
      description: description().value(),
      repositoryUrl: repositoryUrl().value(),
      metaInfo: {
        color: color().value() as string,
      },
    };

    this.store.dispatch(createProject({ project }));

    this.dialogRef.close();
  }
}
