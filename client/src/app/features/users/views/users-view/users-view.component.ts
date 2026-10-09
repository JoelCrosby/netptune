import { Component, computed, inject, signal } from '@angular/core';
import { WorkspaceRole, workspaceRoleLabels } from '@core/enums/workspace-role';
import { DialogService } from '@core/services/dialog.service';
import { UserCommandsService } from '@core/services/user-commands.service';
import { InviteDialogComponent } from '@entry/dialogs/invite-dialog/invite-dialog.component';
import { LucideUser } from '@lucide/angular';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SearchInputComponent } from '@static/components/search-input/search-input.component';
import {
  SelectMenuComponent,
  SelectMenuOption,
} from '@static/components/select-menu/select-menu.component';
import {
  UserListComponent,
  UserRoleFilter,
} from '@users/components/user-list/user-list.component';

// Matches app-filter-action-button so the role menu sits with the other header filters.
const facetButtonClass =
  'hover:bg-foreground/10 text-foreground/70 h-9 min-w-0 gap-2 rounded-sm bg-transparent px-4 text-sm font-normal tracking-normal';

const activeFacetButtonClass =
  'text-primary-400 bg-primary-500/10 hover:bg-primary-500/20';

@Component({
  selector: 'app-users-view',
  templateUrl: './users-view.component.html',
  imports: [
    PageBodyComponent,
    PageContainerComponent,
    PageHeaderComponent,
    SearchInputComponent,
    SelectMenuComponent,
    UserListComponent,
  ],
})
export class UsersViewComponent {
  private dialog = inject(DialogService);
  private userCommands = inject(UserCommandsService);

  readonly search = signal('');
  readonly roleFilter = signal<UserRoleFilter | null>(null);

  protected readonly roleIcon = LucideUser;

  protected readonly roleOptions: SelectMenuOption<UserRoleFilter | null>[] = [
    {
      value: null,
      label: $localize`:Filter option including every role:All`,
    },
    ...[
      WorkspaceRole.owner,
      WorkspaceRole.admin,
      WorkspaceRole.member,
      WorkspaceRole.viewer,
    ].map((role) => ({ value: role, label: workspaceRoleLabels[role] })),
    {
      value: 'pending',
      label: $localize`:Filter option for invited members who have not joined:Pending`,
    },
  ];

  protected readonly roleButtonClass = computed(() => {
    return this.roleFilter() === null
      ? facetButtonClass
      : `${facetButtonClass} ${activeFacetButtonClass}`;
  });

  async onInviteUsers() {
    const result = await this.dialog.openForResult<string[]>(
      InviteDialogComponent,
      { width: '800px' }
    );

    if (!result?.length) return;

    this.userCommands.invite(result);
  }
}
