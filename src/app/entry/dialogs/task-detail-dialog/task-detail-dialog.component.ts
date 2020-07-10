import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  Inject,
  OnDestroy,
  OnInit,
  Optional,
} from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { AppState } from '@core/core.state';
import { TaskStatus } from '@core/enums/project-task-status';
import { Comment } from '@core/models/comment';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import * as ProjectActions from '@core/store/projects/projects.actions';
import * as ProjectSelectors from '@core/store/projects/projects.selectors';
import { Store } from '@ngrx/store';
import * as TaskActions from '@core/store/tasks/tasks.actions';
import * as TaskSelectors from '@core/store/tasks/tasks.selectors';
import { Observable, Subject } from 'rxjs';
import {
  first,
  tap,
  withLatestFrom,
  takeUntil,
  debounceTime,
  filter,
} from 'rxjs/operators';

@Component({
  selector: 'app-task-detail-dialog',
  templateUrl: './task-detail-dialog.component.html',
  styleUrls: ['./task-detail-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskDetailDialogComponent
  implements OnInit, OnDestroy, AfterViewInit {
  task$: Observable<TaskViewModel>;
  projects$: Observable<ProjectViewModel[]>;
  comments$: Observable<Comment[]>;

  selectedTypeValue: number;

  onDestroy$ = new Subject();

  constructor(
    public dialogRef: MatDialogRef<TaskDetailDialogComponent>,
    @Optional() @Inject(MAT_DIALOG_DATA) public data: TaskViewModel,
    private store: Store<AppState>
  ) {}

  projectFromGroup: FormGroup;

  get name() {
    return this.projectFromGroup.get('name');
  }

  get project() {
    return this.projectFromGroup.get('project');
  }

  get description() {
    return this.projectFromGroup.get('description');
  }

  ngOnInit() {
    console.log('ngOnInit');

    this.task$ = this.store.select(TaskSelectors.selectDetailTask).pipe(
      filter((task) => !!task),
      tap((task) => {
        this.buildForm(task);
        this.loadComments(task);
      })
    );

    this.projects$ = this.store.select(ProjectSelectors.selectAllProjects);
  }

  ngAfterViewInit() {
    this.store.dispatch(
      TaskActions.loadTaskDetails({ systemId: this.data.systemId })
    );

    this.store.dispatch(ProjectActions.loadProjects());
  }

  buildForm(task: TaskViewModel) {
    this.projectFromGroup = new FormGroup({
      name: new FormControl(task?.name, {
        updateOn: 'blur',
        validators: [Validators.required, Validators.minLength(4)],
      }),
      project: new FormControl(task?.projectId, {
        updateOn: 'blur',
        validators: [Validators.required],
      }),
      description: new FormControl(task?.description, {
        updateOn: 'blur',
        validators: [],
      }),
    });

    this.monitorInputs(task);
  }

  monitorInputs(task: TaskViewModel) {
    this.name.valueChanges
      .pipe(
        takeUntil(this.onDestroy$),
        debounceTime(300),
        tap((name) => {
          const updated: TaskViewModel = {
            ...task,
            name,
          };

          this.updateTask(updated);
        })
      )
      .subscribe();

    this.project.valueChanges
      .pipe(
        takeUntil(this.onDestroy$),
        debounceTime(300),
        tap((projectId) => {
          const updated: TaskViewModel = {
            ...task,
            projectId,
          };

          this.updateTask(updated);
        })
      )
      .subscribe();

    this.description.valueChanges
      .pipe(
        takeUntil(this.onDestroy$),
        debounceTime(300),
        tap((description) => {
          const updated: TaskViewModel = {
            ...task,
            description,
          };

          this.updateTask(updated);
        })
      )
      .subscribe();
  }

  loadComments(task: TaskViewModel) {}

  updateTask(task: TaskViewModel) {
    this.store.dispatch(
      TaskActions.editProjectTask({
        task,
      })
    );
  }

  close() {
    this.dialogRef.close();
  }

  ngOnDestroy() {
    this.store.dispatch(TaskActions.clearTaskDetail());
    this.onDestroy$.complete();
    this.onDestroy$.unsubscribe();
  }

  getTaskStatus(status: TaskStatus) {
    return TaskStatus[status];
  }

  onFlagClicked() {
    this.task$
      .pipe(
        first(),
        tap((viewModel) => {
          const task: TaskViewModel = {
            ...viewModel,
            isFlagged: !viewModel.isFlagged,
          };

          this.store.dispatch(
            TaskActions.editProjectTask({
              task,
            })
          );
        })
      )
      .subscribe();
  }
}
