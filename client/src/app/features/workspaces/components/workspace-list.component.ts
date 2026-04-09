import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { selectAllWorkspaces } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';
import { CardListComponent } from '@app/static/components/card/card-list.component';
import { CreateWorkspaceListItemComponent } from './create-workspace-list-item.component';
import { WorkspaceListItemComponent } from './workspace-list-item.component';

@Component({
  selector: 'app-workspace-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CardListComponent,
    WorkspaceListItemComponent,
    CreateWorkspaceListItemComponent,
  ],
  template: `
    <app-card-list>
      @for (workspace of workspaces(); track workspace.id) {
        <app-workspace-list-item [workspace]="workspace" />
      }

      <app-create-workspace-list-item />
    </app-card-list>
  `,
})
export class WorkspaceListComponent {
  private store = inject(Store);

  workspaces = this.store.selectSignal(selectAllWorkspaces);
}
