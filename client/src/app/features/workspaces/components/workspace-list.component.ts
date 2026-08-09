import { Component, inject } from '@angular/core';
import { WorkspaceListService } from '@core/services/workspace-list.service';
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
  workspaces = inject(WorkspaceListService).workspaces;
}
