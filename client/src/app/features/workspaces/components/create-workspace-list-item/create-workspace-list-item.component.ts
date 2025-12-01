import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { DialogService } from '@core/services/dialog.service';
import { WorkspaceDialogComponent } from '@entry/dialogs/workspace-dialog/workspace-dialog.component';
import { CardComponent } from '@static/components/card/card.component';
import { CardTitleComponent } from '@static/components/card/card-title.component';
import { CardContentComponent } from '@static/components/card/card-content.component';
import { CardActionsComponent } from '@static/components/card/card-actions.component';
import { MatButton } from '@angular/material/button';

@Component({
  selector: 'app-create-workspace-list-item',
  templateUrl: './create-workspace-list-item.component.html',
  styleUrls: ['./create-workspace-list-item.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CardComponent,
    CardTitleComponent,
    CardContentComponent,
    CardActionsComponent,
    MatButton,
  ],
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
