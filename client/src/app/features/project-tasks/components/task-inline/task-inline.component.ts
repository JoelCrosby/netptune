import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  inject,
  input,
  viewChild
} from '@angular/core';
import {
  FormControl,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
} from '@angular/forms';
import { UserResponse } from '@core/auth/store/auth.models';
import { selectCurrentUser } from '@core/auth/store/auth.selectors';
import { TaskStatus } from '@core/enums/project-task-status';
import { AddProjectTaskRequest } from '@core/models/project-task';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { Workspace } from '@core/models/workspace';
import { selectCurrentProject } from '@core/store/projects/projects.selectors';
import * as TaskActions from '@core/store/tasks/tasks.actions';
import * as TaskSelectors from '@core/store/tasks/tasks.selectors';
import { selectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
import { select, Store } from '@ngrx/store';
import {
  combineLatest,
  fromEvent,
  Observable,
  Subject,
  Subscription,
} from 'rxjs';
import {
  first,
  map,
  shareReplay,
  takeUntil,
  tap,
  throttleTime,
} from 'rxjs/operators';

import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatCheckbox } from '@angular/material/checkbox';
import { MatInput } from '@angular/material/input';

@Component({
  selector: 'app-task-inline',
  templateUrl: './task-inline.component.html',
  styleUrls: ['./task-inline.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatButton,
    MatIcon,
    MatCheckbox,
    FormsModule,
    ReactiveFormsModule,
    MatInput,
  ],
})
export class TaskInlineComponent implements OnInit, OnDestroy {
  private store = inject(Store);
  private cd = inject(ChangeDetectorRef);

  readonly status = input<TaskStatus>(TaskStatus.new);
  readonly siblings = input<TaskViewModel[] | null>();

  readonly containerElementRef = viewChild.required<ElementRef>('taskInlineContainer');
  readonly formElementRef = viewChild.required<ElementRef>('taskInlineForm');
  readonly inputElementRef = viewChild.required<ElementRef>('taskInput');

  outSideClickListener$!: Observable<Event>;

  editActive = false;

  outsideClickSubscription!: Subscription;

  currentWorkspace$!: Observable<Workspace | undefined>;
  currentProject$!: Observable<ProjectViewModel | undefined>;
  currentUser$!: Observable<UserResponse | undefined>;

  inlineEditActive$!: Observable<boolean>;

  taskGroup = new FormGroup({
    taskName: new FormControl(''),
  });

  onDestroy$ = new Subject<void>();

  get taskName() {
    return this.taskGroup.controls.taskName;
  }

  ngOnInit(): void {
    this.currentWorkspace$ = this.store.select(selectCurrentWorkspace);
    this.currentProject$ = this.store.select(selectCurrentProject);
    this.currentUser$ = this.store.select(selectCurrentUser);

    this.inlineEditActive$ = this.store.pipe(
      select(TaskSelectors.selectInlineEditActive),
      map((editActive) => !!editActive),
      shareReplay()
    );

    this.outSideClickListener$ = fromEvent(document, 'mousedown', {
      passive: true,
    }).pipe(
      takeUntil(this.onDestroy$),
      throttleTime(200),
      tap((event) => {
        if (
          this.editActive &&
          !this.containerElementRef().nativeElement.contains(event.target) &&
          !this.formElementRef().nativeElement.contains(event.target)
        ) {
          this.editActive = false;
          this.store.dispatch(
            TaskActions.setInlineEditActive({ active: false })
          );
          this.outsideClickSubscription.unsubscribe();
          this.cd.detectChanges();
        }
      })
    );
  }

  ngOnDestroy() {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  addTaskClicked() {
    this.editActive = true;
    this.store.dispatch(TaskActions.setInlineEditActive({ active: true }));
    this.outsideClickSubscription = this.outSideClickListener$.subscribe();
    this.cd.detectChanges();
    this.inputElementRef().nativeElement.focus();
  }

  onSubmit() {
    combineLatest([
      this.currentWorkspace$,
      this.currentProject$,
      this.currentUser$,
    ])
      .pipe(first())
      .subscribe({
        next: ([workspace, project, user]) => {
          if (!workspace || !project || !user) return;

          this.createTask(workspace, project, user);
        },
      });
  }

  createTask(
    workspace: Workspace,
    project: ProjectViewModel,
    user: UserResponse
  ) {
    const siblings = this.siblings();
    const lastSibling = siblings && siblings[siblings.length - 1];

    const order = lastSibling && lastSibling.sortOrder + 1;

    const task: AddProjectTaskRequest = {
      name: (this.taskName.value as string).trim(),
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

    this.taskGroup.reset();
  }
}
