import { Component, computed, input, output } from '@angular/core';
import { SpinnerComponent } from '@app/static/components/spinner/spinner.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { NotificationViewModel } from '@core/models/view-models/notification-view-model';
import { LucideBell } from '@lucide/angular';
import { NotificationItemComponent } from './notification-item.component';

const DROPDOWN_LIMIT = 10;

@Component({
  selector: 'app-notification-dropdown',
  imports: [
    SpinnerComponent,
    EmptyStateComponent,
    LucideBell,
    NotificationItemComponent,
  ],
  styles: `
    @keyframes dropdown-in {
      from {
        opacity: 0;
        transform: scale(0.95) translateY(-4px);
      }
      to {
        opacity: 1;
        transform: scale(1) translateY(0);
      }
    }

    .animate-dropdown-in {
      animation: dropdown-in 150ms ease-out forwards;
      transform-origin: top right;
    }
  `,
  template: `
    <div
      class="animate-dropdown-in custom-scroll border-border mt-[0.4rem] mr-4 max-h-[80vh] max-w-120 min-w-100 overflow-y-auto rounded-[0.4rem] border bg-white shadow-lg dark:bg-neutral-950">
      <div class="flex items-center justify-between px-[1.2rem] py-3">
        <span class="text-sm font-semibold">Notifications</span>
        @if (unreadCount() > 0) {
          <button
            class="text-muted-foreground hover:text-primary cursor-pointer text-xs underline transition-colors"
            (click)="markAllAsRead.emit()">
            Mark all as read
          </button>
        }
      </div>

      <div class="border-border/50 border-t"></div>

      @if (loaded()) {
        <div class="flex max-h-64 flex-col overflow-auto">
          @for (
            notification of visibleNotifications();
            track notification.id;
            let last = $last
          ) {
            <app-notification-item [notification]="notification" />

            @if (!last) {
              <div class="border-border/50 w-full border-t"></div>
            }
          } @empty {
            <app-empty-state
              compact
              title="No notifications"
              description="You're all caught up!">
              <svg emptyStateIcon lucideBell></svg>
            </app-empty-state>
          }
        </div>

        @if (notifications().length) {
          <div class="border-border/50 sticky bottom-0 border-t bg-inherit">
            <button
              class="hover:text-primary text-muted-foreground w-full cursor-pointer px-[1.2rem] py-3 text-center text-sm font-medium transition-colors"
              (click)="viewAll.emit()">
              View all notifications
            </button>
          </div>
        }
      } @else {
        <div class="flex justify-center p-4">
          <app-spinner diameter="24" />
        </div>
      }
    </div>
  `,
})
export class NotificationDropdownComponent {
  readonly notifications = input.required<NotificationViewModel[]>();
  readonly unreadCount = input.required<number>();
  readonly loaded = input.required<boolean>();
  readonly markAllAsRead = output();
  readonly viewAll = output();

  readonly visibleNotifications = computed(() =>
    this.notifications().slice(0, DROPDOWN_LIMIT)
  );
}
