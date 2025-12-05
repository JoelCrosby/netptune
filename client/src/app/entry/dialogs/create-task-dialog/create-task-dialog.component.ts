import { DialogRef } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
  effect,
  inject,
} from '@angular/core';
import {
  FormControl,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { TaskStatus } from '@core/enums/project-task-status';
import { AddProjectTaskRequest } from '@core/models/project-task';
import { loadProjects } from '@core/store/projects/projects.actions';
import {
  selectAllProjects,
  selectCurrentProjectId,
} from '@core/store/projects/projects.selectors';
import { createProjectTask } from '@core/store/tasks/tasks.actions';
import { selectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { Subject } from 'rxjs';

@Component({
  templateUrl: './create-task-dialog.component.html',
  styleUrls: ['./create-task-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    FormInputComponent,
    FormTextAreaComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    DialogActionsDirective,
    MatButton,
  ],
})
export class CreateTaskDialogComponent implements OnInit, OnDestroy {
  private store = inject(Store);
  dialogRef = inject<DialogRef<CreateTaskDialogComponent>>(DialogRef);

  projects = this.store.selectSignal(selectAllProjects);
  currentWorkspace = this.store.selectSignal(selectCurrentWorkspace);

  selectedTypeValue!: number;

  formGroup = new FormGroup({
    name: new FormControl('', [Validators.required, Validators.minLength(4)]),
    project: new FormControl<number | null | undefined>(null),
    description: new FormControl(''),
  });

  onDestroy$ = new Subject<void>();

  get name() {
    return this.formGroup.controls.name;
  }
  get description() {
    return this.formGroup.controls.description;
  }
  get project() {
    return this.formGroup.controls.project;
  }

  constructor() {
    effect(() => {
      const value = this.store.selectSignal(selectCurrentProjectId);
      this.formGroup.controls.project.setValue(value());
    });
  }

  ngOnInit() {
    this.store.dispatch(loadProjects());
  }

  ngOnDestroy() {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  close() {
    this.dialogRef.close();
  }

  saveClicked() {
    const workspace = this.currentWorkspace();

    if (this.project.value === undefined || this.project.value === null) {
      throw new Error('project id is undefined');
    }

    const task: AddProjectTaskRequest = {
      name: (this.name.value as string).trim(),
      description: (this.description.value as string)?.trim(),
      projectId: this.project.value,
      status: TaskStatus.new,
    };

    if (!workspace?.slug) {
      throw new Error('workspace slug is undefined');
    }

    this.store.dispatch(
      createProjectTask({
        identifier: `[workspace] ${workspace.slug}`,
        task,
      })
    );

    this.dialogRef.close();
  }
}
