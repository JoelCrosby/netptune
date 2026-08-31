import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  NotificationScope,
  NotificationSubscription,
  NotificationSubscriptionEvent,
  hasSubscriptionEvent,
} from '@core/models/notification-subscription';
import { NotificationSubscriptionCommandsService } from '@core/services/notification-subscription-commands.service';
import { NotificationSubscriptionsService } from '@core/services/notification-subscriptions.service';
import { NotificationSubscribeComponent } from '@shared/components/notification-subscribe/notification-subscribe.component';
import { LucideBellPlus, LucideX } from '@lucide/angular';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { IconTileComponent } from '@static/components/icon-tile.component';
import { SkeletonComponent } from '@static/components/skeleton/skeleton.component';

interface SubscriptionRow {
  subscription: NotificationSubscription;
  scopeLabel: string;
  eventsLabel: string;
  removeLabel: string;
}

@Component({
  selector: 'app-notification-subscriptions',
  imports: [
    EmptyStateComponent,
    IconButtonComponent,
    IconTileComponent,
    LucideBellPlus,
    LucideX,
    NotificationSubscribeComponent,
    RouterLink,
    SkeletonComponent,
  ],
  host: { class: 'block' },
  template: `
    <section
      class="border-border bg-card overflow-hidden rounded-lg border shadow-sm">
      <header
        class="border-border flex flex-wrap items-center justify-between gap-x-4 gap-y-3 border-b px-6 py-5">
        <div class="flex min-w-0 items-center gap-3">
          <app-icon-tile [icon]="headingIcon" />

          <div class="min-w-0">
            <h2
              class="font-overpass text-base font-semibold"
              i18n="Heading above the list of places the user follows">
              Places you follow
            </h2>
            <p
              class="text-muted mt-1 text-sm"
              i18n="Explains what the followed places list contains">
              Boards, sprints, groups and projects you asked to hear about.
            </p>
          </div>
        </div>
      </header>

      @if (loading()) {
        <div
          class="flex flex-col gap-5 px-6 py-5"
          role="status"
          i18n-aria-label="Accessible label while followed places load"
          aria-label="Loading the places you follow">
          @for (row of skeletonRows; track $index) {
            <div class="flex items-center justify-between gap-4">
              <div class="flex-1">
                <app-skeleton class="h-3 w-40" />
                <app-skeleton class="mt-2 h-3 w-56" />
              </div>
              <app-skeleton class="h-5 w-9 shrink-0 rounded-full" />
            </div>
          }
        </div>
      } @else {
        @for (row of rows(); track row.subscription.id) {
          <div
            class="border-border flex flex-wrap items-center justify-between gap-x-4 gap-y-2 border-b px-6 py-4 last:border-b-0">
            <div class="min-w-0 flex-1">
              <div class="flex min-w-0 items-center gap-2">
                <span class="text-muted text-xs tracking-wide uppercase">
                  {{ row.scopeLabel }}
                </span>
                <a
                  class="hover:text-primary truncate text-sm font-medium"
                  [routerLink]="row.subscription.link">
                  {{ row.subscription.name }}
                </a>
              </div>

              <p class="text-muted mt-1 text-sm">
                @if (row.subscription.context) {
                  <span>{{ row.subscription.context }} &middot; </span>
                }
                {{ row.eventsLabel }}
              </p>
            </div>

            <div class="flex shrink-0 items-center gap-1">
              <app-notification-subscribe
                [scope]="row.subscription.scope"
                [scopeEntityId]="row.subscription.scopeEntityId"
                [scopeName]="row.subscription.name" />

              <button
                app-icon-button
                type="button"
                [title]="row.removeLabel"
                [attr.aria-label]="row.removeLabel"
                (click)="onRemove(row.subscription)">
                <svg lucideX class="h-4 w-4"></svg>
              </button>
            </div>
          </div>
        } @empty {
          <app-empty-state
            compact
            i18n-title="Empty state for the followed places list"
            title="You are not following anywhere yet."
            i18n-description="Explains how to start following a place"
            description="Use the bell on a board, sprint or project to hear about the tasks in it.">
            <svg emptyStateIcon lucideBellPlus class="h-8 w-8"></svg>
          </app-empty-state>
        }
      }
    </section>
  `,
})
export class NotificationSubscriptionsComponent {
  private readonly commands = inject(NotificationSubscriptionCommandsService);
  private readonly subscriptions = inject(NotificationSubscriptionsService);

  protected readonly headingIcon = LucideBellPlus;
  protected readonly skeletonRows = Array.from({ length: 3 });

  protected readonly loading = computed(() => {
    const isEmpty = !this.subscriptions.subscriptions().length;

    return this.subscriptions.loading() && isEmpty;
  });

  protected readonly rows = computed<SubscriptionRow[]>(() => {
    return this.subscriptions.subscriptions().map((subscription) => {
      return this.toRow(subscription);
    });
  });

  protected onRemove(subscription: NotificationSubscription) {
    this.commands.unsubscribe(subscription).subscribe();
  }

  private toRow(subscription: NotificationSubscription): SubscriptionRow {
    const name = subscription.name;

    return {
      subscription,
      scopeLabel: scopeLabel(subscription.scope),
      eventsLabel: eventsLabel(subscription.events),
      removeLabel: $localize`:Accessible label for the button that stops following a place. NAME is the board, sprint or project name:Stop following ${name}:NAME:`,
    };
  }
}

function scopeLabel(scope: NotificationScope): string {
  switch (scope) {
    case NotificationScope.project:
      return $localize`:Notification subscription scope:Project`;
    case NotificationScope.board:
      return $localize`:Notification subscription scope:Board`;
    case NotificationScope.boardGroup:
      return $localize`:Notification subscription scope:Board group`;
    case NotificationScope.sprint:
      return $localize`:Notification subscription scope:Sprint`;
  }
}

function eventsLabel(events: number): string {
  const labels: string[] = [];

  if (hasSubscriptionEvent(events, NotificationSubscriptionEvent.taskCreated)) {
    labels.push($localize`:Notification subscription event:tasks created`);
  }

  if (hasSubscriptionEvent(events, NotificationSubscriptionEvent.taskUpdated)) {
    labels.push($localize`:Notification subscription event:tasks updated`);
  }

  if (hasSubscriptionEvent(events, NotificationSubscriptionEvent.taskAdded)) {
    labels.push($localize`:Notification subscription event:tasks added`);
  }

  if (hasSubscriptionEvent(events, NotificationSubscriptionEvent.taskRemoved)) {
    labels.push($localize`:Notification subscription event:tasks removed`);
  }

  if (!labels.length) {
    return $localize`:Shown when a followed place has no events selected:nothing selected`;
  }

  return labels.join(', ');
}
