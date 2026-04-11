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

import { FormField, form, required } from '@angular/forms/signals';
import { MatButton } from '@angular/material/button';
import { MatCheckbox } from '@angular/material/checkbox';
import { LucideGripVertical, LucidePlus } from '@lucide/angular';
import { MatInput } from '@angular/material/input';
import { DocumentService } from '@static/services/document.service';

@Component({
  selector: 'app-task-inline',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButton, LucidePlus, LucideGripVertical, MatCheckbox, MatInput, FormField],
  styles: [
    `
      .inline-task-input:-webkit-autofill,
      .inline-task-input:-webkit-autofill:hover,
      .inline-task-input:-webkit-autofill:focus,
      .inline-task-input:-webkit-autofill:active {
        -webkit-transition-delay: 99999s;
      }
    `,
  ],
  template: `
    <div class="flex min-h-[40px] max-h-[40px] w-full flex-row justify-center rounded-sm">
      @if (!isEditActive()) {
        <button
          mat-button
          disableRipple="true"
          class="flex w-full flex-row justify-start rounded-none px-[2.3rem] text-[.8rem] font-medium text-primary hover:bg-primary/10"
          (click)="addTaskClicked()"
        >
          <svg lucidePlus class="h-4 w-4 text-primary"></svg>
          <span class="my-auto mx-4 text-primary">Add Task</span>
        </button>
      } @else {
        <div class="flex w-full flex-row">
          <svg lucideGripVertical class="h-4 w-4 p-2 text-foreground/10 box-content"></svg>
          <mat-checkbox color="primary" disabled />
          <form class="flex h-full w-full flex-row" (submit)="onSubmit($event)">
            <input
              #input
              matInput
              [formField]="taskFrom.name"
              class="inline-task-input w-full border-0 bg-transparent py-0.5 px-5 text-sm text-foreground"
              placeholder="What do you need to get done?"
            />
          </form>
        </div>
      }
    </div>
  `,
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
