import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { Workspace } from '@core/models/workspace';
import * as WorkspaceActions from '@core/store/workspaces/workspaces.actions';
import { selectAllWorkspaces } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-workspace-list',
  templateUrl: './workspace-list.component.html',
  styleUrls: ['./workspace-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkspaceListComponent implements OnInit {
  workspaces$ = this.store.select(selectAllWorkspaces);

  constructor(private store: Store) {}

  ngOnInit() {
    this.store.dispatch(WorkspaceActions.loadWorkspaces());
  }

  trackById(_: number, workspace: Workspace) {
    return workspace.id;
  }
}
