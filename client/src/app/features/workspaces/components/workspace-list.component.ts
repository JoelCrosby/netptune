import { Component, inject } from '@angular/core';
import { selectAllWorkspaces } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';
import { WorkspaceListItemComponent } from './workspace-list-item.component';

@Component({
  selector: 'app-workspace-list',
  imports: [WorkspaceListItemComponent],
  template: `
    <div class="grid grid-cols-1 gap-4 md:grid-cols-2 xl:grid-cols-3">
      @for (workspace of workspaces(); track workspace.id) {
        <app-workspace-list-item [workspace]="workspace" />
      }
    </div>
  `,
})
export class WorkspaceListComponent {
  private store = inject(Store);

  workspaces = this.store.selectSignal(selectAllWorkspaces);
}
