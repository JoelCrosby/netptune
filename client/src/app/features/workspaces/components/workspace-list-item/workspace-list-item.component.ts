import { ChangeDetectionStrategy, Component, OnInit, inject, input } from '@angular/core';
import { DialogService } from '@core/services/dialog.service';
import { Workspace } from '@core/models/workspace';
import * as WorkspaceActions from '@core/store/workspaces/workspaces.actions';
import { HeaderAction } from '@core/types/header-action';
import { WorkspaceDialogComponent } from '@entry/dialogs/workspace-dialog/workspace-dialog.component';
import { Store } from '@ngrx/store';
import { CardListItemComponent } from '@static/components/card-list-item/card-list-item.component';
import { FromNowPipe } from '@static/pipes/from-now.pipe';

@Component({
  selector: 'app-workspace-list-item',
  templateUrl: './workspace-list-item.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CardListItemComponent, FromNowPipe],
})
export class WorkspaceListItemComponent implements OnInit {
  private store = inject(Store);
  private dialog = inject(DialogService);

  readonly workspace = input.required<Workspace>();

  actions!: HeaderAction[];

  ngOnInit() {
    this.actions = [
      {
        label: 'Go To Projects',
        isLink: true,
        routerLink: ['/', this.workspace().slug],
        icon: 'assessment',
      },
      {
        label: 'Manage Users',
        isLink: true,
        routerLink: ['/', this.workspace().slug, 'users'],
      },
      {
        label: 'Edit Workspace',
        click: () => this.onEditClicked(),
      },
    ];
  }

  onEditClicked() {
    this.dialog.open(WorkspaceDialogComponent, {
      data: this.workspace(),
      width: '720px',
    });
  }

  deleteClicked(workspace: Workspace) {
    this.store.dispatch(WorkspaceActions.deleteWorkspace({ workspace }));
  }
}
