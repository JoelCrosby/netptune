import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  input,
} from '@angular/core';
import { Router } from '@angular/router';
import { CardListItemComponent } from '@app/static/components/card/card-list-item.component';
import { Workspace } from '@core/models/workspace';
import { DialogService } from '@core/services/dialog.service';
import * as WorkspaceActions from '@core/store/workspaces/workspaces.actions';
import { HeaderAction } from '@core/types/header-action';
import { WorkspaceDialogComponent } from '@entry/dialogs/workspace-dialog/workspace-dialog.component';
import { LucidePanelsTopLeft } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { FromNowPipe } from '@static/pipes/from-now.pipe';
import { WorkspaceService } from '../../../core/services/workspace.service';

@Component({
  selector: 'app-workspace-list-item',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CardListItemComponent, FromNowPipe],
  template: `
    <app-card-list-item
      [title]="workspace().name"
      [description]="workspace().description"
      [subText]="'Last updated ' + (workspace().updatedAt | fromNow)"
      [actions]="actions"
      (delete)="deleteClicked(workspace())" />
  `,
})
export class WorkspaceListItemComponent implements OnInit {
  private store = inject(Store);
  private dialog = inject(DialogService);

  private workspaceService = inject(WorkspaceService);
  private router = inject(Router);

  readonly workspace = input.required<Workspace>();

  actions!: HeaderAction[];

  ngOnInit() {
    this.actions = [
      {
        label: 'Go To Projects',
        click: () => this.onGotToClicked(),
        icon: LucidePanelsTopLeft,
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

  onGotToClicked() {
    this.workspaceService.setWorkspace(this.workspace().slug ?? null);
    this.router.navigate(['/', this.workspace().slug, 'projects']);
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
