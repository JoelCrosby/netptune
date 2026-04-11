import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { StrokedButtonComponent } from '@app/static/components/button/stroked-button.component';
import { DialogService } from '@core/services/dialog.service';
import { WorkspaceDialogComponent } from '@entry/dialogs/workspace-dialog/workspace-dialog.component';
import { CardActionsComponent } from '@static/components/card/card-actions.component';
import { CardContentComponent } from '@static/components/card/card-content.component';
import { CardTitleComponent } from '@static/components/card/card-title.component';
import { CardComponent } from '@static/components/card/card.component';

@Component({
  selector: 'app-create-workspace-list-item',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CardComponent,
    CardTitleComponent,
    CardContentComponent,
    CardActionsComponent,
    StrokedButtonComponent,
  ],
  template: `
    <app-card class="create-workspace-card">
      <app-card-title>Create a new Workspace</app-card-title>
      <app-card-content>
        <p class="card-text">
          Workspaces allow for team collaboration on multiple projects and are
          the foundation of all workflows within Netptune.
        </p>

        <app-card-actions>
          <button
            app-stroked-button
            color="primary"
            (click)="openWorkspaceDialog()">
            Create Workspace
          </button>
        </app-card-actions>
      </app-card-content>
      <div class="card-footer">
        <small class="text-muted"
          >Created with <span class="text-red">❤</span> by Joel</small
        >
      </div>
    </app-card>
  `,
})
export class CreateWorkspaceListItemComponent {
  private dialog = inject(DialogService);

  openWorkspaceDialog() {
    this.dialog.open(WorkspaceDialogComponent, {
      data: null,
      width: '720px',
    });
  }
}
