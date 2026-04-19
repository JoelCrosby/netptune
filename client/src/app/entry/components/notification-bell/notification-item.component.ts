import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  input,
} from '@angular/core';
import { Router } from '@angular/router';
import { NotificationViewModel } from '@app/core/models/view-models/notification-view-model';
import { markAsRead } from '@app/core/store/notifications/notifications.actions';
import { activityTypeToString } from '@app/core/transforms/activity-type';
import { entityTypeToString } from '@app/core/transforms/entity-type';
import { fromNow } from '@app/core/util/dates';
import { AvatarComponent } from '@app/static/components/avatar/avatar.component';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-notification-item',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AvatarComponent, DatePipe, TooltipDirective],
  template: `
    @if (notification(); as notification) {
      <div
        class="hover:bg-hover flex min-w-80 cursor-pointer flex-row items-center gap-3 px-3 py-3 text-sm"
        [class.opacity-50]="notification.isRead"
        (click)="onNotificationClick()">
        @if (!notification.isRead) {
          <span class="bg-primary h-2 w-2 shrink-0 rounded-full"></span>
        } @else {
          <span class="h-2 w-2 shrink-0"></span>
        }

        <app-avatar
          class="shrink-0 grow-0 basis-8"
          [imageUrl]="notification.actorPictureUrl"
          [name]="notification.actorUsername"
          size="md" />

        <div class="flex flex-1 flex-col gap-1">
          <div class="flex items-center justify-between">
            <span class="font-medium tracking-[0.225px]">
              {{ notification.actorUsername }}
            </span>
            <span
              [appTooltip]="notification.createdAt | date: 'd/M/yy, h:mm a'">
              {{ fromNow(notification.createdAt) }}
            </span>
          </div>
          <span class="text-foreground/70 text-sm">
            {{ activityTypeToString(notification.activityType) }}
            {{ entityTypeToString(notification.entityType) }}
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
      </div>
    }
  `,
})
export class NotificationItemComponent {
  readonly notification = input.required<NotificationViewModel>();
  private store = inject(Store);
  private router = inject(Router);

  readonly activityTypeToString = activityTypeToString;
  readonly entityTypeToString = entityTypeToString;
  readonly fromNow = fromNow;

  onNotificationClick() {
    const notification = this.notification();

    if (!notification.isRead) {
      this.store.dispatch(markAsRead({ id: notification.id }));
    }

    if (notification.link) {
      void this.router.navigateByUrl(notification.link);
    }
  }
}
