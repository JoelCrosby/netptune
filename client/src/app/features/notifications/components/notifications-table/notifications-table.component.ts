import { DatePipe } from '@angular/common';
import {
  Component,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Params, Router } from '@angular/router';
import { NotificationViewModel } from '@core/models/view-models/notification-view-model';
import {
  deleteNotifications,
  markAllAsRead,
  markAsRead,
  markAsReadMany,
} from '@core/store/notifications/notifications.actions';
import { notificationSummary } from '@core/transforms/activity-type';
import { entityTypeToString } from '@core/transforms/entity-type';
import { fromNow } from '@core/util/dates';
import { LucideCheck, LucideExternalLink, LucideTrash2 } from '@lucide/angular';
import { Actions, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import { DatatableDataSource } from '@static/components/datatable/datatable.types';
import { TooltipDirective } from '@static/directives/tooltip.directive';

@Component({
  selector: 'app-notifications-table',
  imports: [
    DatePipe,
    AvatarComponent,
    BadgeComponent,
    TooltipDirective,
    DatatableComponent,
    DatatableCellTemplateDirective,
  ],
  template: `
    <app-datatable
      containerClass="h-[calc(100vh-338px)] min-h-80 overflow-auto"
      tableClass="min-w-[720px] table-fixed"
      rowClass="bg-card"
      [data]="data"
      [stickyHeader]="true"
      [selection]="true"
      (selectionChanged)="selectionChanged.emit($event)"
      (loaded)="loaded.emit($event)">
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
          class="flex min-w-0 items-center truncate text-left text-sm"
          [class.cursor-pointer]="notification.link"
          [class.opacity-60]="notification.isRead"
          (click)="onOpen(notification)">
          {{ notificationSummary(notification) }}
          {{ entityTypeToString(notification.entityType) }}
          @if (notification.entityIdentifier) {
            <span class="font-medium">{{ notification.entityIdentifier }}</span>
          }
          @if (notification.entityName) {
            <span class="text-foreground/60 ml-2">{{
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
          <app-badge shape="rounded">Read</app-badge>
        } @else {
          <app-badge color="primary" shape="rounded">Unread</app-badge>
        }
      </ng-template>

      <div appDatatableEmpty>You're all caught up — no notifications.</div>
    </app-datatable>
  `,
})
export class NotificationsTableComponent {
  private readonly store = inject(Store);
  private readonly actions$ = inject(Actions);
  private readonly router = inject(Router);

  readonly notificationSummary = notificationSummary;
  readonly entityTypeToString = entityTypeToString;
  readonly fromNow = fromNow;

  readonly params = input<Params>({});

  readonly selectionChanged = output<NotificationViewModel[]>();
  readonly loaded = output<{ totalCount: number; hasValue: boolean }>();

  private readonly reload = signal(0);

  private readonly datatable =
    viewChild.required<DatatableComponent<NotificationViewModel>>(
      DatatableComponent
    );

  readonly data: DatatableDataSource<NotificationViewModel> = {
    key: 'notifications-list',
    columns: [
      { id: 'actor', header: 'From', widthClass: 'w-48' },
      { id: 'notification', header: 'Notification', widthClass: 'truncate' },
      { id: 'createdAt', header: 'When', widthClass: 'w-40' },
      { id: 'status', header: 'Status', widthClass: 'w-28' },
    ],
    resource: {
      url: 'api/notifications',
      params: this.params,
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
      {
        label: 'Delete',
        icon: LucideTrash2,
        onClick: (notification) => this.onDelete(notification),
      },
    ],
  };

  constructor() {
    this.actions$
      .pipe(
        ofType(
          markAsRead.success,
          markAllAsRead.success,
          markAsReadMany.success,
          deleteNotifications.success
        ),
        takeUntilDestroyed()
      )
      .subscribe(() => {
        this.datatable().clearSelection();
        this.reload.update((value) => value + 1);
      });
  }

  goToFirstPage() {
    this.datatable().goToPage(1);
  }

  clearSelection() {
    this.datatable().clearSelection();
  }

  onMarkRead(notification: NotificationViewModel) {
    if (notification.isRead) return;

    this.store.dispatch(markAsRead.init({ id: notification.id }));
  }

  onDelete(notification: NotificationViewModel) {
    this.store.dispatch(deleteNotifications.init({ ids: [notification.id] }));
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
