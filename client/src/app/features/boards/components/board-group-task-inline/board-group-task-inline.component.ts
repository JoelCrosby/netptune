import { AfterViewInit, ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, EventEmitter, Input, OnDestroy, OnInit, Output, ViewChild, inject } from '@angular/core';
import {
  FormControl,
  Validators,
  FormsModule,
  ReactiveFormsModule,
} from '@angular/forms';
import * as BoardGroupActions from '@boards/store/groups/board-groups.actions';
import * as BoardGroupSelectors from '@boards/store/groups/board-groups.selectors';
import { UserResponse } from '@core/auth/store/auth.models';
import { selectCurrentUser } from '@core/auth/store/auth.selectors';
import { AddProjectTaskRequest } from '@core/models/project-task';
import { Workspace } from '@core/models/workspace';
import { selectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
import { Actions, ofType } from '@ngrx/effects';
import { Action, Store } from '@ngrx/store';
import {
  BehaviorSubject,
  combineLatest,
  fromEvent,
  Observable,
  Subject,
} from 'rxjs';
import {
  debounceTime,
  first,
  takeUntil,
  tap,
  throttleTime,
} from 'rxjs/operators';
import { MatInput } from '@angular/material/input';
import { CdkTextareaAutosize } from '@angular/cdk/text-field';
import { AsyncPipe } from '@angular/common';
import { MatTooltip } from '@angular/material/tooltip';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';

@Component({
  selector: 'app-board-group-task-inline',
  templateUrl: './board-group-task-inline.component.html',
  styleUrls: ['./board-group-task-inline.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatInput,
    CdkTextareaAutosize,
    FormsModule,
    ReactiveFormsModule,
    MatTooltip,
    SpinnerComponent,
    AsyncPipe
],
})
export class BoardGroupTaskInlineComponent
  implements OnInit, OnDestroy, AfterViewInit
{
  private cd = inject(ChangeDetectorRef);
  private store = inject(Store);
  private actions$ = inject<Actions<Action>>(Actions);

  @ViewChild('taskInput') inputElementRef!: ElementRef;
  @ViewChild('taskInlineContainer') containerElementRef!: ElementRef;

  @Input() boardGroupId!: number;
  @Output() canceled = new EventEmitter();

  taskInputControl = new FormControl<string | null | undefined>(null, [
    Validators.required,
    Validators.maxLength(256),
  ]);

  onDestroy$ = new Subject<void>();

  currentWorkspace$!: Observable<Workspace | undefined>;
  currentProjectId$!: Observable<number | undefined>;
  currentUser$!: Observable<UserResponse | undefined>;
  message$!: Observable<string | null>;

  createInProgress$ = new BehaviorSubject<boolean>(false);

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

    this.store
      .select(BoardGroupSelectors.selectInlineTaskContent)
      .pipe(first())
      .subscribe({
        next: (content) =>
          this.taskInputControl.setValue(content, { emitEvent: false }),
      });

    this.taskInputControl.valueChanges
      .pipe(debounceTime(200), takeUntil(this.onDestroy$))
      .subscribe({
        next: (content) =>
          this.store.dispatch(
            BoardGroupActions.setInlineTaskContent({ content })
          ),
      });
  }

  ngAfterViewInit() {
    this.inputElementRef.nativeElement.focus();

    this.currentWorkspace$ = this.store.select(selectCurrentWorkspace);
    this.currentProjectId$ = this.store.select(
      BoardGroupSelectors.selectBoardProjectId
    );
    this.currentUser$ = this.store.select(selectCurrentUser);
    this.message$ = this.store.select(
      BoardGroupSelectors.selectCreateBoardGroupTaskMessage
    );
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

    combineLatest([this.currentProjectId$, this.currentUser$])
      .pipe(first())
      .subscribe({
        next: ([projectId, user]) => {
          if (!projectId || !user) return;

          this.createTask(projectId, user);
        },
      });
  }

  createTask(projectId: number, user: UserResponse) {
    const task: AddProjectTaskRequest = {
      name: (this.taskInputControl.value as string).trim(),
      projectId,
      assigneeId: user.userId,
      boardGroupId: this.boardGroupId,
    };

    this.store.dispatch(BoardGroupActions.createProjectTask({ task }));

    this.createInProgress$.next(true);
    this.taskInputControl.disable();
  }
}
