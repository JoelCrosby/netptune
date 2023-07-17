import {
  ChangeDetectionStrategy,
  Component,
  Input,
  OnInit,
} from '@angular/core';
import { MatLegacyDialog as MatDialog } from '@angular/material/legacy-dialog';
import { Workspace } from '@core/models/workspace';
import * as WorkspaceActions from '@core/store/workspaces/workspaces.actions';
import { HeaderAction } from '@core/types/header-action';
import { WorkspaceDialogComponent } from '@entry/dialogs/workspace-dialog/workspace-dialog.component';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-workspace-list-item',
  templateUrl: './workspace-list-item.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkspaceListItemComponent implements OnInit {
  @Input() workspace!: Workspace;

  actions!: HeaderAction[];

  constructor(
    private store: Store,
    private dialog: MatDialog
  ) {}

  ngOnInit() {
    this.actions = [
      {
        label: 'Go To Projects',
        isLink: true,
        routerLink: ['/', this.workspace.slug],
        icon: 'assessment',
      },
      {
        label: 'Manage Users',
        isLink: true,
        routerLink: ['/', this.workspace.slug, 'users'],
      },
      {
        label: 'Edit Workspace',
        click: () => this.onEditClicked(),
      },
    ];
  }

  onEditClicked() {
    this.dialog.open(WorkspaceDialogComponent, {
      data: this.workspace,
      width: '720px',
    });
  }

  deleteClicked(workspace: Workspace) {
    this.store.dispatch(WorkspaceActions.deleteWorkspace({ workspace }));
  }
}
