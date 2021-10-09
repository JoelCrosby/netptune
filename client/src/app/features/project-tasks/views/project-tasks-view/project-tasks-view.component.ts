import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
} from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { AppState } from '@core/core.state';
import { exportTasks, loadProjectTasks } from '@core/store/tasks/tasks.actions';
import { ProjectTasksHubService } from '@core/store/tasks/tasks.hub.service';
import { selectTasksLoading } from '@core/store/tasks/tasks.selectors';
import { selectCurrentWorkspaceIdentifier } from '@core/store/workspaces/workspaces.selectors';
import { HeaderAction } from '@core/types/header-action';
import { CreateTaskDialogComponent } from '@entry/dialogs/create-task-dialog/create-task-dialog.component';
import { Store } from '@ngrx/store';
import { from, of } from 'rxjs';
import { first, switchMap } from 'rxjs/operators';

@Component({
  templateUrl: './project-tasks-view.component.html',
  styleUrls: ['./project-tasks-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectTasksViewComponent
  implements OnInit, OnDestroy, AfterViewInit
{
  loading$ = this.store.select(selectTasksLoading);

  secondaryActions: HeaderAction[] = [
    {
      label: 'Export Tasks',
      click: () => this.onExportTasksClicked(),
      icon: 'get_app',
      iconClass: 'material-icons-round',
    },
  ];

  constructor(
    public dialog: MatDialog,
    private store: Store<AppState>,
    private hubService: ProjectTasksHubService
  ) {}

  ngOnInit() {
    this.store
      .select(selectCurrentWorkspaceIdentifier)
      .pipe(
        first(),
        switchMap((identifier) =>
          from(this.hubService.connect()).pipe(
            switchMap(() => {
              if (!identifier) return of({ type: 'NOOP' });

              return this.hubService.addToGroup(identifier);
            })
          )
        )
      )
      .subscribe();
  }

  ngAfterViewInit() {
    this.store.dispatch(loadProjectTasks());
  }

  ngOnDestroy() {
    void this.hubService.disconnect();
  }

  showAddModal() {
    this.dialog.open(CreateTaskDialogComponent, {
      width: '600px',
    });
  }

  onExportTasksClicked() {
    this.store.dispatch(exportTasks());
  }
}
