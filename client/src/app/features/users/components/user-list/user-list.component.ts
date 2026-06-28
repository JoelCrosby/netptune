import { Component, inject, output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { netptunePermissions } from '@app/core/auth/permissions';
import { selectHasPermission } from '@app/core/store/auth/auth.selectors';
import { WorkspaceAppUser } from '@core/models/appuser';
import {
  removeUsersFromWorkspace,
  resendInvite,
} from '@core/store/users/users.actions';
import { LucideTrash2, LucideSend } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import { DatatableDataSource } from '@static/components/datatable/datatable.types';

@Component({
  selector: 'app-user-list',
  imports: [
    RouterLink,
    AvatarComponent,
    DatatableComponent,
    DatatableCellTemplateDirective,
  ],
  template: `
    <app-datatable
      containerClass="h-[calc(100vh-253px)] min-h-80 overflow-auto"
      tableClass="min-w-[720px] table-fixed"
      rowClass="bg-card"
      [data]="userData"
      [customizableColumns]="true"
      [stickyHeader]="true"
      (loaded)="onLoaded($event)">
      <ng-template appDatatableCell="user" let-user>
        <div class="flex min-w-0 items-center gap-3">
          <app-avatar
            class="flex-none"
            [imageUrl]="user.pictureUrl"
            [name]="user.displayName"
            size="sm" />
          <a
            [routerLink]="routerLink(user)"
            class="min-w-0 truncate text-sm font-medium"
            [class.pointer-events-none]="!routerLink(user)">
            {{ user.displayName }}
          </a>
        </div>
      </ng-template>

      <ng-template appDatatableCell="email" let-user>
        <a
          [routerLink]="routerLink(user)"
          class="text-foreground/60 block truncate text-sm"
          [class.pointer-events-none]="!routerLink(user)">
          {{ user.email }}
        </a>
      </ng-template>

      <ng-template appDatatableCell="status" let-user>
        @if (user.isPending) {
          <span
            class="inline-flex items-center rounded bg-yellow-100 px-2 py-0.5 text-xs font-medium text-yellow-700">
            Pending
          </span>
        } @else if (user.isWorkspaceOwner) {
          <span
            class="inline-flex items-center rounded bg-blue-100 px-2 py-0.5 text-xs font-medium text-blue-700">
            Owner
          </span>
        } @else {
          <span
            class="inline-flex items-center rounded bg-neutral-100 px-2 py-0.5 text-xs font-medium text-neutral-600">
            Member
          </span>
        }
      </ng-template>
    </app-datatable>
  `,
})
export class UserListComponent {
  private store = inject(Store);

  readonly countChange = output<number>();

  canReadUsers = this.store.selectSignal(
    selectHasPermission(netptunePermissions.members.read)
  );

  readonly userData: DatatableDataSource<WorkspaceAppUser> = {
    key: 'user-list',
    columns: [
      { id: 'user', header: 'User', sortable: true, widthClass: 'w-64' },
      { id: 'email', header: 'Email', sortable: true },
      { id: 'status', header: 'Status', sortable: true, widthClass: 'w-32' },
    ],
    resource: {
      url: 'api/users',
      params: signal({}),
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, user: WorkspaceAppUser) => user.id,
    menu: [
      {
        label: 'Resend invite',
        icon: LucideSend,
        onClick: (user) => this.onResendClicked(user),
      },
      {
        label: 'Remove user from workspace',
        icon: LucideTrash2,
        onClick: (user) => this.onRemoveClicked(user),
      },
    ],
  };

  onLoaded(event: { totalCount: number; hasValue: boolean }) {
    if (event.hasValue) {
      this.countChange.emit(event.totalCount);
    }
  }

  routerLink(user: WorkspaceAppUser) {
    if (user.isPending) return null;
    return this.canReadUsers() ? ['.', user.id] : null;
  }

  onRemoveClicked(user: WorkspaceAppUser) {
    if (!user || user.isWorkspaceOwner) return;
    this.store.dispatch(
      removeUsersFromWorkspace.init({ emailAddresses: [user.email] })
    );
  }

  onResendClicked(user: WorkspaceAppUser) {
    if (!user?.email || !user.isPending) return;
    this.store.dispatch(resendInvite.init({ email: user.email }));
  }
}
