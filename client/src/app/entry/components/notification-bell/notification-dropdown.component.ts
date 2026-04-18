import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';
import { SpinnerComponent } from '@app/static/components/spinner/spinner.component';
import { NotificationViewModel } from '@core/models/view-models/notification-view-model';
import { LucideBell } from '@lucide/angular';
import { NotificationItemComponent } from './notification-item.component';

@Component({
  selector: 'app-notification-dropdown',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [SpinnerComponent, LucideBell, NotificationItemComponent],
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
        @for (
          notification of notifications();
          track notification.id;
          let last = $last
        ) {
          <app-notification-item [notification]="notification" />

          @if (!last) {
            <div class="border-border/50 w-full border-t"></div>
          }
        } @empty {
          <div
            class="flex min-w-80 flex-col items-center gap-4 px-[0.8rem] py-[0.4rem] text-sm">
            <svg lucideBell></svg>
            <span>No notifications</span>
            <p class="text-foreground/60">You're all caught up!</p>
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
  readonly markAllAsRead = output<void>();
}
