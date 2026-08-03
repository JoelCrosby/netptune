import { httpResource } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Page } from '@app/core/models/pagination';
import { ClientResponse } from '@core/models/client-response';
import { NotificationViewModel } from '@core/models/view-models/notification-view-model';
import { selectUnreadCount } from '@core/store/notifications/notifications.selectors';
import { LucideBell } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { IconTileComponent } from '@static/components/icon-tile.component';
import { NotificationListComponent } from '@static/components/notification-list.component';
import { SkeletonComponent } from '@static/components/skeleton/skeleton.component';

const pageSize = 20;

@Component({
  selector: 'app-dashboard-notifications-card',
  imports: [
    BadgeComponent,
    IconTileComponent,
    NotificationListComponent,
    RouterLink,
    SkeletonComponent,
    StrokedButtonComponent,
  ],
  template: `
    <section
      class="border-border bg-card flex h-full min-h-24 flex-col overflow-hidden rounded-lg border shadow-sm lg:absolute lg:inset-0">
      <header
        class="border-border flex shrink-0 flex-wrap items-center justify-between gap-x-4 gap-y-2 border-b px-6 py-5">
        <div class="flex min-w-0 items-center gap-3">
          <app-icon-tile [icon]="notificationIcon" />

          <div class="flex min-w-0 items-center gap-2">
            <h3
              class="font-overpass text-base font-semibold"
              i18n="Heading of the dashboard notifications card">
              Notifications
            </h3>

            @if (unreadCount() > 0) {
              <app-badge color="primary" class="tabular-nums">
                {{ unreadLabel() }}
              </app-badge>
            }
          </div>
        </div>

        <a
          class="text-primary shrink-0 text-sm font-medium hover:underline"
          [routerLink]="['../notifications']">
          <span i18n="Link to the full notifications page">View all</span>
        </a>
      </header>

      @if (isInitialLoad()) {
        <div
          class="flex flex-col gap-4 px-6 py-5"
          role="status"
          i18n-aria-label="Accessible label while notifications load"
          aria-label="Loading notifications">
          @for (row of skeletonRows; track $index) {
            <div class="flex items-center gap-3">
              <app-skeleton class="h-8 w-8 shrink-0 rounded-full" />
              <div class="flex-1">
                <app-skeleton class="h-3 w-32" />
                <app-skeleton class="mt-2 h-3 w-full" />
              </div>
            </div>
          }
        </div>
      } @else {
        <div class="custom-scroll min-h-0 flex-1 overflow-y-auto py-2">
          <app-notification-list [notifications]="visible()" />
        </div>

        @if (hasMore()) {
          <div class="border-border shrink-0 border-t px-6 py-4">
            <button
              app-stroked-button
              color="primary"
              type="button"
              class="w-full"
              [disabled]="resource.isLoading()"
              (click)="loadMore()">
              <span i18n="Button that loads more notifications">Load more</span>
            </button>
          </div>
        }
      }
    </section>
  `,
})
export class DashboardNotificationsCardComponent {
  private readonly store = inject(Store);
  private readonly take = signal(pageSize);

  protected readonly notificationIcon = LucideBell;
  protected readonly skeletonRows = Array.from({ length: 4 });
  protected readonly unreadCount = this.store.selectSignal(selectUnreadCount);

  readonly resource = httpResource<ClientResponse<Page<NotificationViewModel>>>(
    () => ({
      url: 'api/notifications',
      params: { take: this.take() + 1 },
    })
  );

  private readonly items = computed(
    () => this.resource.value()?.payload?.items ?? []
  );

  readonly visible = computed(() => this.items().slice(0, this.take()));
  readonly hasMore = computed(() => this.items().length > this.take());

  readonly isInitialLoad = computed(
    () => this.resource.isLoading() && this.items().length === 0
  );

  protected readonly unreadLabel = computed(() => {
    const count = this.unreadCount();

    return $localize`:Number of unread notifications shown on the dashboard card:${count}:COUNT: unread`;
  });

  loadMore() {
    this.take.update((take) => take + pageSize);
  }
}
