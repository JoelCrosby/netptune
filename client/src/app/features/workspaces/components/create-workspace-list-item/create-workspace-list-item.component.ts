import { ChangeDetectionStrategy, Component } from '@angular/core';
import { DialogService } from '@core/services/dialog.service';
import { WorkspaceDialogComponent } from '@entry/dialogs/workspace-dialog/workspace-dialog.component';

@Component({
    selector: 'app-create-workspace-list-item',
    templateUrl: './create-workspace-list-item.component.html',
    styleUrls: ['./create-workspace-list-item.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false
})
export class CreateWorkspaceListItemComponent {
  constructor(private dialog: DialogService) {}

  openWorkspaceDialog() {
    this.dialog.open(WorkspaceDialogComponent, {
      data: null,
      width: '720px',
    });
  }
}
