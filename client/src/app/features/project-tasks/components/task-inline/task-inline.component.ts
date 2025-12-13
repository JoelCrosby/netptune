import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  effect,
  ElementRef,
  inject,
  input,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { UserResponse } from '@core/auth/store/auth.models';
import { selectCurrentUser } from '@core/auth/store/auth.selectors';
import { TaskStatus } from '@core/enums/project-task-status';
import { AddProjectTaskRequest } from '@core/models/project-task';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { Workspace } from '@core/models/workspace';
import { selectCurrentProject } from '@core/store/projects/projects.selectors';
import * as TaskActions from '@core/store/tasks/tasks.actions';
import { selectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';

import { Field, form, required } from '@angular/forms/signals';
import { MatButton } from '@angular/material/button';
import { MatCheckbox } from '@angular/material/checkbox';
import { MatIcon } from '@angular/material/icon';
import { MatInput } from '@angular/material/input';
import { DocumentService } from '@static/services/document.service';

@Component({
  selector: 'app-task-inline',
  templateUrl: './task-inline.component.html',
  styleUrls: ['./task-inline.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButton, MatIcon, MatCheckbox, MatInput, Field],
})
export class TaskInlineComponent {
  private store = inject(Store);
  private document = inject(DocumentService);
  private cd = inject(ChangeDetectorRef);
  private elementRef = inject(ElementRef);

  readonly status = input<TaskStatus>(TaskStatus.new);
  readonly siblings = input<TaskViewModel[] | null>();
  readonly inputElementRef = viewChild<ElementRef>('input');

  currentWorkspace = this.store.selectSignal(selectCurrentWorkspace);
  currentProject = this.store.selectSignal(selectCurrentProject);
  currentUser = this.store.selectSignal(selectCurrentUser);

  isEditActive = signal(false);

  taskFormModel = signal({
    name: '',
  });

  taskFrom = form(this.taskFormModel, (schema) => {
    required(schema.name);
  });

  constructor() {
    effect(() => {
      const el = this.document.documentClicked();
      untracked(() => this.handleDocumentClick(el));
    });
  }

  handleDocumentClick(target: EventTarget) {
    if (this.isEditActive()) {
      if (!this.elementRef.nativeElement.contains(target)) {
        return this.isEditActive.set(false);
      }
    } else {
      if (this.elementRef.nativeElement.contains(target)) {
        this.isEditActive.set(true);
        this.focusInput();
      }
    }
  }

  focusInput() {
    this.cd.detectChanges();
    const textarea = this.inputElementRef();

    if (textarea) {
      textarea?.nativeElement.focus();
    }
  }

  addTaskClicked() {
    this.store.dispatch(TaskActions.setInlineEditActive({ active: true }));
    this.focusInput();
  }

  onSubmit(event: Event) {
    event.preventDefault();

    const workspace = this.currentWorkspace();
    const project = this.currentProject();
    const user = this.currentUser();

    if (!workspace || !project || !user) {
      return;
    }

    this.createTask(workspace, project, user);
  }

  createTask(
    workspace: Workspace,
    project: ProjectViewModel,
    user: UserResponse
  ) {
    if (this.taskFrom().invalid()) {
      return;
    }

    const siblings = this.siblings();
    const lastSibling = siblings && siblings[siblings.length - 1];

    const order = lastSibling && lastSibling.sortOrder + 1;
    const name = this.taskFrom.name().value();

    const task: AddProjectTaskRequest = {
      name: name.trim(),
      projectId: project.id,
      status: this.status(),
      sortOrder: order || 1,
      assigneeId: user.userId,
    };

    if (!workspace.slug) {
      return;
    }

    this.store.dispatch(
      TaskActions.createProjectTask({
        identifier: `[workspace] ${workspace.slug}`,
        task,
      })
    );

    this.taskFormModel.set({ name: '' });
    this.taskFrom().reset();
  }
}
