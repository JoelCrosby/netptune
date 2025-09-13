import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Workspace } from '@core/models/workspace';
import { selectAllWorkspaces } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';
import { CardListComponent } from '@static/components/card-list/card-list.component';
import { AsyncPipe } from '@angular/common';
import { WorkspaceListItemComponent } from '../workspace-list-item/workspace-list-item.component';
import { CreateWorkspaceListItemComponent } from '../create-workspace-list-item/create-workspace-list-item.component';

@Component({
  selector: 'app-workspace-list',
  templateUrl: './workspace-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CardListComponent,
    WorkspaceListItemComponent,
    CreateWorkspaceListItemComponent,
    AsyncPipe
],
})
export class WorkspaceListComponent {
  private store = inject(Store);

  workspaces$ = this.store.select(selectAllWorkspaces);

  trackById(_: number, workspace: Workspace) {
    return workspace.id;
  }
}
