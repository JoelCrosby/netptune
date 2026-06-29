import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { Params } from '@angular/router';
import { AppUser } from '@core/models/appuser';
import { NotificationViewModel } from '@core/models/view-models/notification-view-model';
import {
  deleteNotifications,
  markAllAsRead,
  markAsReadMany,
} from '@core/store/notifications/notifications.actions';
import { loadUsers } from '@core/store/users/users.actions';
import { selectAllUsers } from '@core/store/users/users.selectors';
import { Store } from '@ngrx/store';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { NotificationsFiltersComponent } from '../../components/notifications-filters/notifications-filters.component';
import { NotificationsTableComponent } from '../../components/notifications-table/notifications-table.component';

@Component({
  selector: 'app-notifications-view',
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    NotificationsFiltersComponent,
    NotificationsTableComponent,
  ],
  template: `
    <app-page-container>
      <app-page-header
        title="Notifications"
        [count]="count()"
        [actionTitle]="count() ? 'Mark all as read' : null"
        (actionClick)="onMarkAllAsRead()" />

      <app-notifications-filters
        [searchTerm]="searchTerm()"
        [users]="users()"
        [selectedUsers]="selectedUsers()"
        [selectedCount]="selected().length"
        (searchChange)="onSearch($event)"
        (userFilter)="onUserFilter($event)"
        (clearUserFilter)="onClearUserFilter()"
        (markSelectedAsRead)="onMarkSelectedAsRead()"
        (deleteSelected)="onDeleteSelected()" />

      <app-notifications-table
        [params]="resourceParams()"
        (selectionChanged)="selected.set($event)"
        (loaded)="onLoaded($event)" />
    </app-page-container>
  `,
})
export class NotificationsViewComponent {
  private readonly store = inject(Store);

  readonly count = signal<number | null>(null);
  readonly selected = signal<NotificationViewModel[]>([]);
  readonly searchTerm = signal<string | null>(null);
  readonly selectedUsers = signal<AppUser[]>([]);
  readonly users = this.store.selectSignal(selectAllUsers);

  private readonly table = viewChild.required(NotificationsTableComponent);

  readonly resourceParams = computed<Params>(() => {
    const params: Params = {};
    const search = this.searchTerm();
    const userId = this.selectedUsers()[0]?.id;

    if (search) params['search'] = search;
    if (userId) params['userId'] = userId;

    return params;
  });

  constructor() {
    this.store.dispatch(loadUsers.init());
  }

  onMarkAllAsRead() {
    this.store.dispatch(markAllAsRead.init());
  }

  onSearch(term: string | null) {
    this.searchTerm.set(term);
    this.table().goToFirstPage();
  }

  onUserFilter(user: AppUser) {
    const current = this.selectedUsers()[0];

    this.selectedUsers.set(current?.id === user.id ? [] : [user]);
    this.table().goToFirstPage();
  }

  onClearUserFilter() {
    this.selectedUsers.set([]);
    this.table().goToFirstPage();
  }

  onLoaded(event: { totalCount: number; hasValue: boolean }) {
    if (event.hasValue) {
      this.count.set(event.totalCount);
    }
  }

  onMarkSelectedAsRead() {
    const ids = this.selected()
      .filter((notification) => !notification.isRead)
      .map((notification) => notification.id);

    if (!ids.length) {
      this.table().clearSelection();
      return;
    }

    this.store.dispatch(markAsReadMany.init({ ids }));
  }

  onDeleteSelected() {
    const ids = this.selected().map((notification) => notification.id);

    if (!ids.length) return;

    this.store.dispatch(deleteNotifications.init({ ids }));
  }
}
