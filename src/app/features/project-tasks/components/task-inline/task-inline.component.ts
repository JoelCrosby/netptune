import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  ElementRef,
  Input,
  OnDestroy,
  OnInit,
  ViewChild,
} from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
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
  shareReplay,
  takeUntil,
  tap,
  throttleTime,
} from 'rxjs/operators';

@Component({
  selector: 'app-task-inline',
  templateUrl: './task-inline.component.html',
  styleUrls: ['./task-inline.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskInlineComponent implements OnInit, OnDestroy {
  @Input() status: TaskStatus = TaskStatus.New;
  @Input() siblings: TaskViewModel[];

  outSideClickListener$: Observable<Event>;

  editActive = false;

  outsideClickSubscription: Subscription;

  currentWorkspace$: Observable<Workspace>;
  currentProject$: Observable<ProjectViewModel>;
  inlineEditActive$: Observable<boolean>;
  currentUser$: Observable<UserResponse>;

  taskGroup = new FormGroup({
    taskName: new FormControl(),
  });

  onDestroy$ = new Subject();

  get taskName() {
    return this.taskGroup.get('taskName');
  }

  @ViewChild('taskInlineContainer') containerElementRef: ElementRef;
  @ViewChild('taskInlineForm') formElementRef: ElementRef;
  @ViewChild('taskInput') inputElementRef: ElementRef;

  constructor(private store: Store, private cd: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.currentWorkspace$ = this.store.pipe(select(selectCurrentWorkspace));
    this.currentProject$ = this.store.pipe(select(selectCurrentProject));
    this.currentUser$ = this.store.pipe(select(selectCurrentUser));

    this.inlineEditActive$ = this.store.pipe(
      select(TaskSelectors.selectInlineEditActive),
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
          !this.containerElementRef.nativeElement.contains(event.target) &&
          !this.formElementRef.nativeElement.contains(event.target)
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
    this.inputElementRef.nativeElement.focus();
  }

  onSubmit() {
    combineLatest([
      this.currentWorkspace$,
      this.currentProject$,
      this.currentUser$,
    ])
      .pipe(first())
      .subscribe({
        next: ([workspace, project, user]) =>
          this.createTask(workspace, project, user),
      });
  }

  createTask(
    workspace: Workspace,
    project: ProjectViewModel,
    user: UserResponse
  ) {
    const lastSibling =
      this.siblings && this.siblings[this.siblings.length - 1];

    const order = lastSibling && lastSibling.sortOrder + 1;

    const task: AddProjectTaskRequest = {
      name: (this.taskName.value as string).trim(),
      workspace: workspace.slug,
      projectId: project.id,
      status: this.status,
      sortOrder: order || 1,
      assigneeId: user.userId,
    };

    this.store.dispatch(
      TaskActions.createProjectTask({ identifier: '[none]', task })
    );

    this.taskGroup.reset();
  }
}
