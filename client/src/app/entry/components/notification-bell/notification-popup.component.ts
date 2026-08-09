import { Component, computed, input, output } from '@angular/core';
import { NotificationViewModel } from '@core/models/view-models/notification-view-model';
import {
  notificationNamesEntity,
  notificationSummary,
} from '@core/transforms/activity-type';
import { entityTypeToString } from '@core/transforms/entity-type';
import { LucideBell } from '@lucide/angular';
import { AnchoredPopupCardComponent } from '@static/components/anchored-popup/anchored-popup-card.component';

@Component({
  selector: 'app-notification-popup',
  imports: [LucideBell, AnchoredPopupCardComponent],
  template: `
    <app-anchored-popup-card (dismissed)="dismissed.emit()">
      <svg popupIcon lucideBell class="h-4 w-4"></svg>

      <p
        class="text-sm font-medium"
        i18n="Heading of the popup shown when a notification arrives">
        New notification
      </p>

      <p class="text-muted mt-0.5 line-clamp-3 text-sm">{{ summary() }}</p>

      <button
        type="button"
        class="text-primary mt-2 text-sm font-medium hover:underline"
        i18n="
          Button that opens the notification from the new notification popup
        "
        (click)="opened.emit()">
        View
      </button>
    </app-anchored-popup-card>
  `,
})
export class NotificationPopupComponent {
  readonly notification = input.required<NotificationViewModel>();

  readonly opened = output();
  readonly dismissed = output();

  protected readonly summary = computed(() => {
    const notification = this.notification();

    const namesEntity = notificationNamesEntity(notification);
    const parts = [
      notification.actorUsername,
      notificationSummary(notification),
      namesEntity ? entityTypeToString(notification.entityType) : null,
      namesEntity ? notification.entityIdentifier : null,
      namesEntity ? notification.entityName : null,
    ];

    return parts.filter(Boolean).join(' ');
  });
}
