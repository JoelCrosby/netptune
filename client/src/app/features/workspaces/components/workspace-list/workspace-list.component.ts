import { ChangeDetectionStrategy, Component } from '@angular/core';
import { AppState } from '@core/core.state';
import { Workspace } from '@core/models/workspace';
import { selectAllWorkspaces } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-workspace-list',
  templateUrl: './workspace-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkspaceListComponent {
  workspaces$ = this.store.select(selectAllWorkspaces);

  constructor(private store: Store<AppState>) {}

  trackById(_: number, workspace: Workspace) {
    return workspace.id;
  }
}
