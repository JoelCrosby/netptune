import { selectCurrentUser } from '@core/auth/store/auth.selectors';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import {
  ChangeDetectorRef,
  Component,
  ElementRef,
  Input,
  OnDestroy,
  OnInit,
  ViewChild,
  ChangeDetectionStrategy,
} from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { AppState } from '@core/core.state';
import { TaskStatus } from '@core/enums/project-task-status';
import { AddProjectTaskRequest } from '@core/models/project-task';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { Workspace } from '@core/models/workspace';
import { SelectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
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
import * as TaskSelectors from '@core/store/tasks/tasks.selectors';
import * as TaskActions from '@core/store/tasks/tasks.actions';
import { User } from '@core/auth/store/auth.models';
import { selectCurrentProject } from '@core/store/projects/projects.selectors';

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
  currentUser$: Observable<User>;

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

  constructor(private store: Store<AppState>, private cd: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.currentWorkspace$ = this.store.pipe(select(SelectCurrentWorkspace));
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

  createTask(workspace: Workspace, project: ProjectViewModel, user: User) {
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

    this.store.dispatch(TaskActions.createProjectTask({ task }));

    this.taskGroup.reset();
  }
}
