import {
  ChangeDetectionStrategy,
  Component,
  Input,
  OnInit,
} from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Workspace } from '@core/models/workspace';
import * as WorkspaceActions from '@core/store/workspaces/workspaces.actions';
import { WorkspaceDialogComponent } from '@entry/dialogs/workspace-dialog/workspace-dialog.component';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-workspace-list-item',
  templateUrl: './workspace-list-item.component.html',
  styleUrls: ['./workspace-list-item.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkspaceListItemComponent implements OnInit {
  @Input() workspace: Workspace;

  constructor(private store: Store, private dialog: MatDialog) {}

  ngOnInit(): void {}

  onEditClicked(workspace: Workspace) {
    this.dialog.open(WorkspaceDialogComponent, {
      data: workspace,
      width: '720px',
    });
  }

  deleteClicked(workspace: Workspace) {
    this.store.dispatch(WorkspaceActions.deleteWorkspace({ workspace }));
  }
}
