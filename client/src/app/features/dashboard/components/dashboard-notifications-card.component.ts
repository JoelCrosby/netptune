import { httpResource } from '@angular/common/http';
import { Component, computed, signal } from '@angular/core';
import { NotificationItemComponent } from '@app/entry/components/notification-bell/notification-item.component';
import { ClientResponse } from '@core/models/client-response';
import { NotificationViewModel } from '@core/models/view-models/notification-view-model';
import { LucideBell } from '@lucide/angular';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';

const PAGE_SIZE = 20;

@Component({
  selector: 'app-dashboard-notifications-card',
  imports: [
    NotificationItemComponent,
    StrokedButtonComponent,
    SpinnerComponent,
    LucideBell,
  ],
  template: `
    <div
      class="border-border bg-card flex h-full min-h-24 flex-col overflow-hidden rounded border p-6 shadow-sm lg:absolute lg:inset-0">
      <div class="mb-2 flex items-center justify-between">
        <h3 class="text-foreground text-base font-semibold">Notifications</h3>
      </div>

      @if (isInitialLoad()) {
        <div class="flex flex-1 items-center justify-center">
          <app-spinner diameter="24" />
        </div>
      } @else {
        <div class="custom-scroll -mx-3 min-h-0 flex-1 overflow-y-auto">
          <ul class="divide-border/50 flex flex-col divide-y">
            @for (notification of visible(); track notification.id) {
              <li>
                <app-notification-item [notification]="notification" />
              </li>
            } @empty {
              <div
                class="text-muted flex min-h-40 flex-col items-center justify-center gap-2 text-sm">
                <svg lucideBell></svg>
                <span>You're all caught up!</span>
              </div>
            }
          </ul>
        </div>

        @if (hasMore()) {
          <button
            app-stroked-button
            color="primary"
            type="button"
            class="mt-4 w-full shrink-0"
            [disabled]="resource.isLoading()"
            (click)="loadMore()">
            Load more
          </button>
        }
      }
    </div>
  `,
})
export class DashboardNotificationsCardComponent {
  // Number of notifications currently requested for display. Grows by a page
  // each time the user loads more.
  private readonly take = signal(PAGE_SIZE);

  // Request one extra row so we can tell whether a further page exists without
  // needing a separate total-count round trip.
  readonly resource = httpResource<ClientResponse<NotificationViewModel[]>>(
    () => ({
      url: 'api/notifications',
      params: { take: this.take() + 1 },
    })
  );

  private readonly items = computed(() => this.resource.value()?.payload ?? []);

  readonly visible = computed(() => this.items().slice(0, this.take()));

  readonly hasMore = computed(() => this.items().length > this.take());

  readonly isInitialLoad = computed(
    () => this.resource.isLoading() && this.items().length === 0
  );

  loadMore() {
    this.take.update((take) => take + PAGE_SIZE);
  }
}
