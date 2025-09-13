import { DialogRef } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component, OnDestroy, inject } from '@angular/core';
import {
  FormControl,
  FormGroup,
  Validators,
  FormsModule,
  ReactiveFormsModule,
} from '@angular/forms';
import { AddProjectRequest } from '@core/models/project';
import { createProject } from '@core/store/projects/projects.actions';
import { selectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';
import { Subscription } from 'rxjs';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { MatButton } from '@angular/material/button';

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
export class ProjectDialogComponent implements OnDestroy {
  private store = inject(Store);
  dialogRef = inject<DialogRef<ProjectDialogComponent>>(DialogRef);

  currentWorkspace$ = this.store.select(selectCurrentWorkspace);
  subs = new Subscription();

  projectFromGroup = new FormGroup({
    name: new FormControl('', [Validators.required, Validators.minLength(4)]),
    repositoryUrl: new FormControl(),
    description: new FormControl(),
    workspace: new FormControl(),
    color: new FormControl('#673AB7'),
  });

  get name() {
    return this.projectFromGroup.controls.name;
  }
  get description() {
    return this.projectFromGroup.controls.description;
  }
  get repositoryUrl() {
    return this.projectFromGroup.controls.repositoryUrl;
  }
  get color() {
    return this.projectFromGroup.controls.color;
  }

  ngOnDestroy() {
    this.subs.unsubscribe();
  }

  close() {
    this.dialogRef.close();
  }

  getResult() {
    this.subs = this.currentWorkspace$.subscribe({
      next: (workspace) => {
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
      },
    });
  }
}
