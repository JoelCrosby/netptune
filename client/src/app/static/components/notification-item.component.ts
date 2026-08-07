import { DatePipe } from '@angular/common';
import { Component, computed, inject, input } from '@angular/core';
import { Router } from '@angular/router';
import { NotificationViewModel } from '@app/core/models/view-models/notification-view-model';
import { markAsRead } from '@app/core/store/notifications/notifications.actions';
import {
  notificationNamesEntity,
  notificationSummary,
} from '@app/core/transforms/activity-type';
import { entityTypeToString } from '@app/core/transforms/entity-type';
import { fromNow } from '@app/core/util/dates';
import { AvatarComponent } from '@app/static/components/avatar/avatar.component';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-notification-item',
  imports: [AvatarComponent, DatePipe, TooltipDirective],
  template: `
    @if (notification(); as notification) {
      <button
        type="button"
        [class]="buttonClass()"
        (click)="onNotificationClick()">
        @if (!notification.isRead) {
          <span class="bg-primary mt-2 h-2 w-2 shrink-0 rounded-full"></span>
        } @else {
          <span class="mt-2 h-2 w-2 shrink-0"></span>
        }

        <app-avatar
          class="shrink-0 grow-0 basis-8"
          [imageUrl]="notification.actorPictureUrl"
          [name]="notification.actorUsername"
          [isServiceAccount]="notification.actorIsServiceAccount ?? false"
          size="md" />

        <div class="flex min-w-0 flex-1 flex-col gap-1">
          <div class="flex items-baseline justify-between gap-2">
            <span class="truncate font-medium tracking-[0.225px]">
              {{ notification.actorUsername }}
            </span>
            <span
              class="text-muted shrink-0 text-xs"
              [appTooltip]="notification.createdAt | date: 'd/M/yy, h:mm a'">
              {{ fromNow(notification.createdAt) }}
            </span>
          </div>
          <span class="text-foreground/70 text-sm">
            {{ notificationSummary(notification) }}
            @if (notificationNamesEntity(notification.activityType)) {
              {{ entityTypeToString(notification.entityType) }}
            }
            @if (notification.entityIdentifier) {
              <span class="text-foreground/85 font-medium">
                {{ notification.entityIdentifier }}
              </span>
            }
            @if (notification.entityName) {
              <span class="text-foreground/60">
                {{ notification.entityName }}
              </span>
            }
          </span>
        </div>
      </button>
    }
  `,
})
export class NotificationItemComponent {
  readonly notification = input.required<NotificationViewModel>();
  private store = inject(Store);
  private router = inject(Router);

  readonly notificationSummary = notificationSummary;
  readonly notificationNamesEntity = notificationNamesEntity;
  readonly entityTypeToString = entityTypeToString;
  readonly fromNow = fromNow;

  protected readonly buttonClass = computed(() => {
    const base =
      'hover:bg-hover focus-visible:ring-primary flex w-full min-w-80 cursor-pointer flex-row items-start gap-3 px-4 py-3 text-left text-sm transition-colors focus-visible:-outline-offset-2 focus-visible:ring-2 focus-visible:outline-none';

    return this.notification().isRead ? base : `${base} bg-primary/4`;
  });

  onNotificationClick() {
    const notification = this.notification();

    if (!notification.isRead) {
      this.store.dispatch(markAsRead.init({ id: notification.id }));
    }

    void this.router.navigateByUrl(notification.link);
  }
}
