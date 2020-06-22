import {
  AfterViewInit,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  ElementRef,
  EventEmitter,
  OnDestroy,
  OnInit,
  Output,
  ViewChild,
  Input,
} from '@angular/core';
import {
  fromEvent,
  Subject,
  Subscription,
  combineLatest,
  Observable,
  BehaviorSubject,
} from 'rxjs';
import { takeUntil, tap, throttleTime, first } from 'rxjs/operators';
import { ProjectViewModel } from '@app/core/models/view-models/project-view-model';
import { Workspace } from '@app/core/models/workspace';
import { User } from '@app/core/auth/store/auth.models';
import { select, Store, Action } from '@ngrx/store';
import { SelectCurrentWorkspace } from '@app/core/workspaces/workspaces.selectors';
import { selectCurrentProject } from '@app/core/projects/projects.selectors';
import { selectCurrentUser } from '@app/core/auth/store/auth.selectors';
import { AddProjectTaskRequest } from '@app/core/models/project-task';
import { AppState } from '@app/core/core.state';
import { FormControl } from '@angular/forms';
import * as TaskActions from '@project-tasks/store/tasks.actions';
import * as BoardGroupActions from '@boards/store/groups/board-groups.actions';
import { Actions, ofType } from '@ngrx/effects';

@Component({
  selector: 'app-board-group-task-inline',
  templateUrl: './board-group-task-inline.component.html',
  styleUrls: ['./board-group-task-inline.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardGroupTaskInlineComponent
  implements OnInit, OnDestroy, AfterViewInit {
  @ViewChild('taskInput') inputElementRef: ElementRef;
  @ViewChild('taskInlineContainer') containerElementRef: ElementRef;

  @Input() boardGroupId: number;
  @Output() canceled = new EventEmitter();

  taskInputControl = new FormControl();

  onDestroy$ = new Subject();
  outsideClickSubscription: Subscription;

  currentWorkspace$: Observable<Workspace>;
  currentProject$: Observable<ProjectViewModel>;
  currentUser$: Observable<User>;

  createInProgress$ = new BehaviorSubject<boolean>(false);

  constructor(
    private cd: ChangeDetectorRef,
    private store: Store<AppState>,
    private actions$: Actions<Action>
  ) {}

  ngOnInit() {
    fromEvent(document, 'mousedown', {
      passive: true,
    })
      .pipe(
        takeUntil(this.onDestroy$),
        throttleTime(200),
        tap(this.handleDocumentClick.bind(this))
      )
      .subscribe();

    this.actions$
      .pipe(
        takeUntil(this.onDestroy$),
        ofType(BoardGroupActions.loadBoardGroupsSuccess),
        tap(() => {
          this.createInProgress$.next(false);
          this.taskInputControl.reset();
          this.taskInputControl.enable();
          this.inputElementRef.nativeElement.focus();
        })
      )
      .subscribe();
  }

  ngAfterViewInit() {
    this.inputElementRef.nativeElement.focus();

    this.currentWorkspace$ = this.store.pipe(select(SelectCurrentWorkspace));
    this.currentProject$ = this.store.pipe(select(selectCurrentProject));
    this.currentUser$ = this.store.pipe(select(selectCurrentUser));
  }

  ngOnDestroy() {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  handleDocumentClick(event: Event) {
    if (!this.containerElementRef.nativeElement.contains(event.target)) {
      this.canceled.emit();
      this.cd.detectChanges();
    }
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
    const task: AddProjectTaskRequest = {
      name: (this.taskInputControl.value as string).trim(),
      workspace: workspace.slug,
      projectId: project.id,
      assigneeId: user.userId,
      boardGroupId: this.boardGroupId,
    };

    this.store.dispatch(TaskActions.createProjectTask({ task }));

    this.createInProgress$.next(true);
    this.taskInputControl.disable();
  }
}
