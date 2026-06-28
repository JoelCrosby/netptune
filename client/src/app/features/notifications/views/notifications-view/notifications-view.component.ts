import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { NotificationViewModel } from '@core/models/view-models/notification-view-model';
import {
  markAllAsRead,
  markAsRead,
} from '@core/store/notifications/notifications.actions';
import { activityTypeToString } from '@core/transforms/activity-type';
import { entityTypeToString } from '@core/transforms/entity-type';
import { fromNow } from '@core/util/dates';
import { LucideCheck, LucideExternalLink } from '@lucide/angular';
import { Actions, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import { DatatableDataSource } from '@static/components/datatable/datatable.types';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';

@Component({
  selector: 'app-notifications-view',
  imports: [
    DatePipe,
    AvatarComponent,
    TooltipDirective,
    PageContainerComponent,
    PageHeaderComponent,
    DatatableComponent,
    DatatableCellTemplateDirective,
  ],
  template: `
    <app-page-container>
      <app-page-header
        title="Notifications"
        [count]="count()"
        [actionTitle]="count() ? 'Mark all as read' : null"
        (actionClick)="onMarkAllAsRead()" />

      <app-datatable
        containerClass="h-[calc(100vh-253px)] min-h-80 overflow-auto"
        tableClass="min-w-[720px] table-fixed"
        rowClass="bg-card"
        [data]="data"
        [stickyHeader]="true"
        (loaded)="onLoaded($event)">
        <ng-template appDatatableCell="actor" let-notification>
          <div class="flex min-w-0 items-center gap-3">
            <app-avatar
              class="flex-none"
              [imageUrl]="notification.actorPictureUrl"
              [name]="notification.actorUsername"
              size="sm" />
            <span class="min-w-0 truncate text-sm font-medium">
              {{ notification.actorUsername }}
            </span>
          </div>
        </ng-template>

        <ng-template appDatatableCell="notification" let-notification>
          <button
            type="button"
            class="block min-w-0 truncate text-left text-sm"
            [class.cursor-pointer]="notification.link"
            [class.opacity-60]="notification.isRead"
            (click)="onOpen(notification)">
            {{ activityTypeToString(notification.activityType) }}
            {{ entityTypeToString(notification.entityType) }}
            @if (notification.entityIdentifier) {
              <span class="font-medium">{{
                notification.entityIdentifier
              }}</span>
            }
            @if (notification.entityName) {
              <span class="text-foreground/60">{{
                notification.entityName
              }}</span>
            }
          </button>
        </ng-template>

        <ng-template appDatatableCell="createdAt" let-notification>
          <span
            class="text-foreground/60 text-sm"
            [appTooltip]="notification.createdAt | date: 'd/M/yy, h:mm a'">
            {{ fromNow(notification.createdAt) }}
          </span>
        </ng-template>

        <ng-template appDatatableCell="status" let-notification>
          @if (notification.isRead) {
            <span
              class="inline-flex items-center rounded bg-neutral-100 px-2 py-0.5 text-xs font-medium text-neutral-600">
              Read
            </span>
          } @else {
            <span
              class="bg-primary/10 text-primary inline-flex items-center rounded px-2 py-0.5 text-xs font-medium">
              Unread
            </span>
          }
        </ng-template>

        <div appDatatableEmpty>You're all caught up — no notifications.</div>
      </app-datatable>
    </app-page-container>
  `,
})
export class NotificationsViewComponent {
  private readonly store = inject(Store);
  private readonly actions$ = inject(Actions);
  private readonly router = inject(Router);

  readonly activityTypeToString = activityTypeToString;
  readonly entityTypeToString = entityTypeToString;
  readonly fromNow = fromNow;

  readonly count = signal<number | null>(null);
  private readonly reload = signal(0);

  readonly data: DatatableDataSource<NotificationViewModel> = {
    key: 'notifications-list',
    columns: [
      { id: 'actor', header: 'From', widthClass: 'w-48' },
      { id: 'notification', header: 'Notification' },
      { id: 'createdAt', header: 'When', widthClass: 'w-40' },
      { id: 'status', header: 'Status', widthClass: 'w-28' },
    ],
    resource: {
      url: 'api/notifications',
      params: signal({}),
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, notification: NotificationViewModel) =>
      notification.id,
    reloadSignal: this.reload,
    menu: [
      {
        label: 'Mark as read',
        icon: LucideCheck,
        onClick: (notification) => this.onMarkRead(notification),
      },
      {
        label: 'Open',
        icon: LucideExternalLink,
        onClick: (notification) => this.onOpen(notification),
      },
    ],
  };

  constructor() {
    this.actions$
      .pipe(
        ofType(markAsRead.success, markAllAsRead.success),
        takeUntilDestroyed()
      )
      .subscribe(() => this.reload.update((value) => value + 1));
  }

  onMarkAllAsRead() {
    this.store.dispatch(markAllAsRead.init());
  }

  onLoaded(event: { totalCount: number; hasValue: boolean }) {
    if (event.hasValue) {
      this.count.set(event.totalCount);
    }
  }

  onMarkRead(notification: NotificationViewModel) {
    if (notification.isRead) return;

    this.store.dispatch(markAsRead.init({ id: notification.id }));
  }

  onOpen(notification: NotificationViewModel) {
    if (!notification.isRead) {
      this.store.dispatch(markAsRead.init({ id: notification.id }));
    }

    if (notification.link) {
      void this.router.navigateByUrl(notification.link);
    }
  }
}
