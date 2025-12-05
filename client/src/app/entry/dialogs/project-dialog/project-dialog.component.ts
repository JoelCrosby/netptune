import { DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import {
  FormControl,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { AddProjectRequest } from '@core/models/project';
import { createProject } from '@core/store/projects/projects.actions';
import { selectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';

@Component({
  selector: 'app-project-dialog',
  templateUrl: './project-dialog.component.html',
  styleUrls: ['./project-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    FormInputComponent,
    FormTextAreaComponent,
    DialogActionsDirective,
    MatButton,
  ],
})
export class ProjectDialogComponent {
  private store = inject(Store);
  dialogRef = inject<DialogRef<ProjectDialogComponent>>(DialogRef);

  currentWorkspace = this.store.selectSignal(selectCurrentWorkspace);

  form = new FormGroup({
    name: new FormControl('', [Validators.required, Validators.minLength(4)]),
    repositoryUrl: new FormControl(),
    description: new FormControl(),
    workspace: new FormControl(),
    color: new FormControl('#673AB7'),
  });

  get name() {
    return this.form.controls.name;
  }
  get description() {
    return this.form.controls.description;
  }
  get repositoryUrl() {
    return this.form.controls.repositoryUrl;
  }
  get color() {
    return this.form.controls.color;
  }

  close() {
    this.dialogRef.close();
  }

  getResult() {
    const workspace = this.currentWorkspace();

    if (!workspace?.slug) return;

    const project: AddProjectRequest = {
      name: this.name.value as string,
      description: this.description.value,
      repositoryUrl: this.repositoryUrl.value,
      metaInfo: {
        color: this.color.value as string,
      },
    };

    this.store.dispatch(createProject({ project }));

    this.dialogRef.close();
  }
}
