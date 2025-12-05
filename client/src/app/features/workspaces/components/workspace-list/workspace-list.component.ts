import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { selectAllWorkspaces } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';
import { CardListComponent } from '@static/components/card-list/card-list.component';
import { CreateWorkspaceListItemComponent } from '../create-workspace-list-item/create-workspace-list-item.component';
import { WorkspaceListItemComponent } from '../workspace-list-item/workspace-list-item.component';

@Component({
  selector: 'app-workspace-list',
  templateUrl: './workspace-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CardListComponent,
    WorkspaceListItemComponent,
    CreateWorkspaceListItemComponent,
  ],
})
export class WorkspaceListComponent {
  private store = inject(Store);

  workspaces = this.store.selectSignal(selectAllWorkspaces);
}
