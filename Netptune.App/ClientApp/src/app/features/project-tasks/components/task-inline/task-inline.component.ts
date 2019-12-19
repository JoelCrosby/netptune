import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { AppState } from '@core/core.state';
import { TaskStatus } from '@core/enums/project-task-status';
import { Project } from '@core/models/project';
import { AddProjectTaskRequest } from '@core/models/project-task';
import { Workspace } from '@core/models/workspace';
import { SelectCurrentProject } from '@core/state/core.selectors';
import { SelectCurrentWorkspace } from '@core/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';
import { combineLatest, Observable, Subscription } from 'rxjs';
import { first } from 'rxjs/operators';
import { createProjectTask } from './../../store/tasks.actions';

@Component({
  selector: 'app-task-inline',
  templateUrl: './task-inline.component.html',
  styleUrls: ['./task-inline.component.scss'],
})
export class TaskInlineComponent implements OnInit, OnDestroy {
  @Input() status: TaskStatus = TaskStatus.New;

  editMode = false;
  subs = new Subscription();

  currentWorkspace$: Observable<Workspace>;
  currentProject$: Observable<Project>;

  taskGroup = new FormGroup({
    taskName: new FormControl(),
  });

  get taskName() {
    return this.taskGroup.get('taskName');
  }

  constructor(private store: Store<AppState>) {}

  ngOnInit(): void {
    this.currentWorkspace$ = this.store.select(SelectCurrentWorkspace);
    this.currentProject$ = this.store.select(SelectCurrentProject);
  }

  ngOnDestroy() {
    this.subs.unsubscribe();
  }

  addTaskClicked() {
    this.editMode = true;
  }

  onSubmit() {
    combineLatest([this.currentWorkspace$, this.currentProject$])
      .pipe(first())
      .subscribe({
        next: ([workspace, project]) => {
          console.log({ workspace, project });

          const task: AddProjectTaskRequest = {
            name: this.taskName.value,
            workspace: workspace.slug,
            projectId: project.id,
            status: this.status,
          };

          this.store.dispatch(createProjectTask({ task }));

          this.taskGroup.reset();
        },
      });
  }
}
