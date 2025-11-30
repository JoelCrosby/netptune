import { CdkTextareaAutosize } from '@angular/cdk/text-field';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  ElementRef,
  inject,
  input,
  OnDestroy,
  OnInit,
  output,
  signal,
  viewChild,
} from '@angular/core';
import {
  FormControl,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MatInput } from '@angular/material/input';
import { MatTooltip } from '@angular/material/tooltip';
import * as BoardGroupActions from '@boards/store/groups/board-groups.actions';
import * as BoardGroupSelectors from '@boards/store/groups/board-groups.selectors';
import {
  selectBoardProjectId,
  selectCreateBoardGroupTaskMessage,
} from '@boards/store/groups/board-groups.selectors';
import { UserResponse } from '@core/auth/store/auth.models';
import { selectCurrentUser } from '@core/auth/store/auth.selectors';
import { AddProjectTaskRequest } from '@core/models/project-task';
import { selectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
import { Actions, ofType } from '@ngrx/effects';
import { Action, Store } from '@ngrx/store';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import { fromEvent, Subject } from 'rxjs';
import {
  debounceTime,
  first,
  takeUntil,
  tap,
  throttleTime,
} from 'rxjs/operators';

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
  ],
})
export class BoardGroupTaskInlineComponent
  implements OnInit, OnDestroy, AfterViewInit
{
  private cd = inject(ChangeDetectorRef);
  private store = inject(Store);
  private actions$ = inject<Actions<Action>>(Actions);

  readonly inputElementRef = viewChild.required<ElementRef>('taskInput');
  readonly containerElementRef = viewChild.required<ElementRef>(
    'taskInlineContainer'
  );

  readonly boardGroupId = input.required<number>();
  readonly canceled = output();

  taskInputControl = new FormControl<string | null | undefined>(null, [
    Validators.required,
    Validators.maxLength(256),
  ]);

  onDestroy$ = new Subject<void>();

  currentWorkspace = this.store.selectSignal(selectCurrentWorkspace);
  currentProjectId = this.store.selectSignal(selectBoardProjectId);
  currentUser = this.store.selectSignal(selectCurrentUser);
  message = this.store.selectSignal(selectCreateBoardGroupTaskMessage);

  createInProgress = signal(false);

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
          this.createInProgress.set(false);
          this.taskInputControl.reset();
          this.taskInputControl.enable();
          this.inputElementRef().nativeElement.focus();
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
    this.inputElementRef().nativeElement.focus();
  }

  ngOnDestroy() {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  handleDocumentClick(event: Event) {
    if (!this.containerElementRef().nativeElement.contains(event.target)) {
      this.canceled.emit();
      this.cd.detectChanges();
    }
  }

  onSubmit(event?: Event) {
    event?.preventDefault();

    const user = this.currentUser();
    const projectId = this.currentProjectId();

    if (!projectId || !user) return;

    this.createTask(projectId, user);
  }

  createTask(projectId: number, user: UserResponse) {
    const task: AddProjectTaskRequest = {
      name: (this.taskInputControl.value as string).trim(),
      projectId,
      assigneeId: user.userId,
      boardGroupId: this.boardGroupId(),
    };

    this.store.dispatch(BoardGroupActions.createProjectTask({ task }));

    this.createInProgress.set(true);
    this.taskInputControl.disable();
  }
}
