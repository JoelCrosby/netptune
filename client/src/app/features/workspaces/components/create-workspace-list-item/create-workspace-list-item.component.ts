import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatLegacyDialog as MatDialog } from '@angular/material/legacy-dialog';
import { WorkspaceDialogComponent } from '@entry/dialogs/workspace-dialog/workspace-dialog.component';

@Component({
  selector: 'app-create-workspace-list-item',
  templateUrl: './create-workspace-list-item.component.html',
  styleUrls: ['./create-workspace-list-item.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CreateWorkspaceListItemComponent {
  constructor(private dialog: MatDialog) {}

  openWorkspaceDialog() {
    this.dialog.open(WorkspaceDialogComponent, {
      data: null,
      width: '720px',
    });
  }
}
