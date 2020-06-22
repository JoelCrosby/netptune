import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  Inject,
  OnInit,
  Optional,
  OnDestroy,
} from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { AppState } from '@app/core/core.state';
import { TaskViewModel } from '@app/core/models/view-models/project-task-dto';
import { ProjectViewModel } from '@app/core/models/view-models/project-view-model';
import * as ProjectActions from '@app/core/projects/projects.actions';
import * as ProjectSelectors from '@app/core/projects/projects.selectors';
import * as TaskActions from '@app/features/project-tasks/store/tasks.actions';
import * as TaskSelectors from '@app/features/project-tasks/store/tasks.selectors';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { tap, first } from 'rxjs/operators';
import { TaskStatus } from '@app/core/enums/project-task-status';

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

  selectedTypeValue: number;

  constructor(
    public dialogRef: MatDialogRef<TaskDetailDialogComponent>,
    @Optional() @Inject(MAT_DIALOG_DATA) public data: TaskViewModel,
    private store: Store<AppState>
  ) {}

  projectFromGroup: FormGroup;

  ngOnInit() {
    this.task$ = this.store
      .select(TaskSelectors.selectDetailTask)
      .pipe(tap((task) => this.buildForm(task)));

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
      nameFormControl: new FormControl(task?.name, [
        Validators.required,
        Validators.minLength(4),
      ]),
      projectFormControl: new FormControl(task?.projectId),
      descriptionFormControl: new FormControl(task?.description),
    });
  }

  close() {
    this.dialogRef.close();
  }

  ngOnDestroy() {
    this.store.dispatch(TaskActions.clearTaskDetail());
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
