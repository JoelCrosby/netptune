import {
  AfterViewInit,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  OnDestroy,
  OnInit,
  Output,
  ViewChild,
} from '@angular/core';
import { FormControl, Validators } from '@angular/forms';
import { UserResponse } from '@core/auth/store/auth.models';
import { selectCurrentUser } from '@core/auth/store/auth.selectors';
import { AddProjectTaskRequest } from '@core/models/project-task';
import { Workspace } from '@core/models/workspace';
import * as BoardGroupActions from '@boards/store/groups/board-groups.actions';
import * as BoardGroupSelectors from '@boards/store/groups/board-groups.selectors';
import { selectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
import { Actions, ofType } from '@ngrx/effects';
import { Action, select, Store } from '@ngrx/store';
import {
  BehaviorSubject,
  combineLatest,
  fromEvent,
  Observable,
  Subject,
  Subscription,
} from 'rxjs';
import { first, takeUntil, tap, throttleTime } from 'rxjs/operators';

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

  taskInputControl = new FormControl(null, [
    Validators.required,
    Validators.maxLength(256),
  ]);

  onDestroy$ = new Subject();
  outsideClickSubscription: Subscription;

  currentWorkspace$: Observable<Workspace>;
  currentProjectId$: Observable<number>;
  currentUser$: Observable<UserResponse>;

  createInProgress$ = new BehaviorSubject<boolean>(false);

  constructor(
    private cd: ChangeDetectorRef,
    private store: Store,
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

    this.currentWorkspace$ = this.store.pipe(select(selectCurrentWorkspace));
    this.currentProjectId$ = this.store.pipe(
      select(BoardGroupSelectors.selectBoardProjectId)
    );
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

  onSubmit(event?: Event) {
    event?.preventDefault();

    combineLatest([
      this.currentWorkspace$,
      this.currentProjectId$,
      this.currentUser$,
    ])
      .pipe(first())
      .subscribe({
        next: ([workspace, projectId, user]) =>
          this.createTask(workspace, projectId, user),
      });
  }

  createTask(workspace: Workspace, projectId: number, user: UserResponse) {
    const task: AddProjectTaskRequest = {
      name: (this.taskInputControl.value as string).trim(),
      projectId,
      workspace: workspace.slug,
      assigneeId: user.userId,
      boardGroupId: this.boardGroupId,
    };

    this.store.dispatch(BoardGroupActions.createProjectTask({ task }));

    this.createInProgress$.next(true);
    this.taskInputControl.disable();
  }
}
