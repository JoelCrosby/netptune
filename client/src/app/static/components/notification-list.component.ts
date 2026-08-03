import { Component, input } from '@angular/core';
import { NotificationViewModel } from '@core/models/view-models/notification-view-model';
import { LucideBell } from '@lucide/angular';
import { EmptyStateComponent } from './empty-state/empty-state.component';
import { NotificationItemComponent } from './notification-item.component';

@Component({
  selector: 'app-notification-list',
  imports: [EmptyStateComponent, LucideBell, NotificationItemComponent],
  host: { class: 'block' },
  template: `
    <ul class="divide-border/50 flex flex-col divide-y">
      @for (notification of notifications(); track notification.id) {
        <li>
          <app-notification-item [notification]="notification" />
        </li>
      } @empty {
        <li>
          <app-empty-state
            compact
            i18n-title="Heading of the empty notifications list"
            title="No notifications"
            i18n-description="Reassurance shown when there are no notifications"
            description="You're all caught up!">
            <svg emptyStateIcon lucideBell></svg>
          </app-empty-state>
        </li>
      }
    </ul>
  `,
})
export class NotificationListComponent {
  readonly notifications = input.required<readonly NotificationViewModel[]>();
}
