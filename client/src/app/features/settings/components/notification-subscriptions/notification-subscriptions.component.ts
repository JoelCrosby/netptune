import {
  Component,
  computed,
  inject,
  linkedSignal,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  NotificationScope,
  NotificationSubscription,
  NotificationSubscriptionEvent,
  hasSubscriptionEvent,
  toggleSubscriptionEvent,
} from '@core/models/notification-subscription';
import { NotificationSubscriptionCommandsService } from '@core/services/notification-subscription-commands.service';
import { NotificationSubscriptionsService } from '@core/services/notification-subscriptions.service';
import { LucideBellPlus, LucideX } from '@lucide/angular';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { FilterInputComponent } from '@static/components/filter-input/filter-input.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import {
  SegmentedControlComponent,
  SegmentedOption,
} from '@static/components/segmented-control/segmented-control.component';
import { SkeletonComponent } from '@static/components/skeleton/skeleton.component';
import { PanelComponent } from '@static/components/panel.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';

type ScopeFilter = 'all' | `${NotificationScope}`;

interface EventChip {
  event: NotificationSubscriptionEvent;
  label: string;
  isSelected: boolean;
  // The server refuses a subscription with no events, so the last one stays on.
  isLocked: boolean;
}

interface SubscriptionRow {
  subscription: NotificationSubscription;
  scopeLabel: string;
  removeLabel: string;
  events: EventChip[];
}

const EVENT_LABELS: { event: NotificationSubscriptionEvent; label: string }[] =
  [
    {
      event: NotificationSubscriptionEvent.taskCreated,
      label: $localize`:Notification subscription event:tasks created`,
    },
    {
      event: NotificationSubscriptionEvent.taskUpdated,
      label: $localize`:Notification subscription event:tasks updated`,
    },
    {
      event: NotificationSubscriptionEvent.taskAdded,
      label: $localize`:Notification subscription event:tasks added`,
    },
    {
      event: NotificationSubscriptionEvent.taskRemoved,
      label: $localize`:Notification subscription event:tasks removed`,
    },
  ];

const SCOPE_FILTER_OPTIONS: SegmentedOption<ScopeFilter>[] = [
  {
    value: 'all',
    label: $localize`:Filter showing followed places of every scope:All`,
  },
  {
    value: `${NotificationScope.project}`,
    label: scopeLabel(NotificationScope.project),
  },
  {
    value: `${NotificationScope.board}`,
    label: scopeLabel(NotificationScope.board),
  },
  {
    value: `${NotificationScope.boardGroup}`,
    label: scopeLabel(NotificationScope.boardGroup),
  },
  {
    value: `${NotificationScope.sprint}`,
    label: scopeLabel(NotificationScope.sprint),
  },
];

@Component({
  selector: 'app-notification-subscriptions',
  imports: [
    BadgeComponent,
    EmptyStateComponent,
    FilterInputComponent,
    IconButtonComponent,
    LucideBellPlus,
    LucideX,
    PanelComponent,
    PanelHeaderComponent,
    RouterLink,
    SegmentedControlComponent,
    SkeletonComponent,
  ],
  host: { class: 'block' },
  template: `
    <section app-panel surface="card">
      <app-panel-header
        density="comfortable"
        [icon]="headingIcon"
        i18n-heading="Heading above the list of places the user follows"
        heading="Places you follow"
        i18n-description="Explains what the followed places list contains"
        description="Boards, sprints, groups and projects you asked to hear about.">
        @if (!loading() && hasSubscriptions()) {
          <span panelHeaderActions class="text-muted text-xs">
            {{ countLabel() }}
          </span>
        }
      </app-panel-header>

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
      } @else if (!hasSubscriptions()) {
        <app-empty-state
          compact
          i18n-title="Empty state for the followed places list"
          title="You are not following anywhere yet."
          i18n-description="Explains how to start following a place"
          description="Use the bell on a board, sprint or project to hear about the tasks in it.">
          <svg emptyStateIcon lucideBellPlus class="h-8 w-8"></svg>
        </app-empty-state>
      } @else {
        <div
          class="border-border flex flex-wrap items-center gap-2.5 border-b px-6 py-3">
          <app-filter-input
            class="min-w-48 flex-1"
            [(value)]="query"
            i18n-placeholder="Placeholder in the followed places search box"
            placeholder="Search the places you follow" />

          <app-segmented-control
            class="shrink-0"
            [options]="scopeFilterOptions"
            [(value)]="scopeFilter"
            i18n-ariaLabel="
              Accessible label for the control that filters followed places by
              scope
            "
            ariaLabel="Show places" />
        </div>

        @for (row of rows(); track row.subscription.id) {
          <div
            class="border-border flex items-center gap-4 border-b px-6 py-3.5 last:border-b-0">
            <div class="min-w-0 flex-1">
              <div class="flex min-w-0 items-center gap-2">
                <app-badge
                  shape="rounded"
                  class="text-muted shrink-0 text-[10px] font-bold tracking-wider uppercase">
                  {{ row.scopeLabel }}
                </app-badge>
                <a
                  class="hover:text-primary truncate text-sm font-semibold"
                  [routerLink]="row.subscription.link">
                  {{ row.subscription.name }}
                </a>
                @if (row.subscription.context) {
                  <span class="text-muted truncate text-xs">
                    &middot; {{ row.subscription.context }}
                  </span>
                }
              </div>

              <div class="mt-2 flex flex-wrap items-center gap-1.5">
                @for (chip of row.events; track chip.event) {
                  <button
                    type="button"
                    class="focus-visible:ring-primary h-6 cursor-pointer rounded-full border px-2.5 text-xs font-medium transition-colors outline-none focus-visible:ring-2 disabled:cursor-default"
                    [class]="chipClass(chip.isSelected)"
                    [attr.aria-pressed]="chip.isSelected"
                    [disabled]="chip.isLocked"
                    (click)="onEventToggled(row.subscription, chip.event)">
                    {{ chip.label }}
                  </button>
                }
              </div>
            </div>

            <button
              app-icon-button
              type="button"
              class="shrink-0"
              [title]="row.removeLabel"
              [attr.aria-label]="row.removeLabel"
              (click)="onRemove(row.subscription)">
              <svg lucideX class="h-4 w-4"></svg>
            </button>
          </div>
        } @empty {
          <app-empty-state
            compact
            i18n-title="Empty state when no followed places match the search"
            title="Nothing here matches that"
            i18n-description="
              Suggests how to find followed places after a search found nothing
            "
            description="Try another name, or show all scopes." />
        }
      }
    </section>
  `,
})
export class NotificationSubscriptionsComponent {
  private readonly commands = inject(NotificationSubscriptionCommandsService);
  private readonly subscriptions = inject(NotificationSubscriptionsService);

  protected readonly headingIcon = LucideBellPlus;
  protected readonly scopeFilterOptions = SCOPE_FILTER_OPTIONS;
  protected readonly skeletonRows = Array.from({ length: 3 });

  protected readonly query = signal('');
  protected readonly scopeFilter = signal<ScopeFilter>('all');

  // Events toggled here show at once; the stored list catches up after the save refreshes it.
  private readonly pendingEvents = linkedSignal<
    NotificationSubscription[],
    ReadonlyMap<number, number>
  >({
    source: () => this.subscriptions.subscriptions(),
    computation: (subscriptions, previous) => {
      const pending = new Map(previous?.value ?? []);

      subscriptions.forEach((subscription) => {
        if (pending.get(subscription.id) === subscription.events) {
          pending.delete(subscription.id);
        }
      });

      return pending;
    },
  });

  protected readonly loading = computed(() => {
    const isEmpty = !this.subscriptions.subscriptions().length;

    return this.subscriptions.loading() && isEmpty;
  });

  protected readonly hasSubscriptions = computed(() => {
    return this.subscriptions.subscriptions().length > 0;
  });

  protected readonly countLabel = computed(() => {
    const count = this.subscriptions.subscriptions().length;

    return count === 1
      ? $localize`:Count of followed places when there is one:1 place`
      : $localize`:Count of followed places. COUNT is a number:${count}:COUNT: places`;
  });

  protected readonly rows = computed<SubscriptionRow[]>(() => {
    const query = this.query().trim().toLowerCase();
    const scopeFilter = this.scopeFilter();
    const pending = this.pendingEvents();

    return this.subscriptions
      .subscriptions()
      .filter((subscription) => {
        const matchesScope =
          scopeFilter === 'all' || `${subscription.scope}` === scopeFilter;
        const haystack =
          `${subscription.name} ${subscription.context ?? ''}`.toLowerCase();

        return matchesScope && (!query || haystack.includes(query));
      })
      .map((subscription) => {
        const events = pending.get(subscription.id) ?? subscription.events;

        return this.toRow(subscription, events);
      });
  });

  protected chipClass(isSelected: boolean): string {
    return isSelected
      ? 'bg-primary/10 text-primary border-transparent'
      : 'border-border text-muted hover:text-foreground hover:bg-hover bg-transparent';
  }

  protected onEventToggled(
    subscription: NotificationSubscription,
    event: NotificationSubscriptionEvent
  ) {
    const current =
      this.pendingEvents().get(subscription.id) ?? subscription.events;
    const events = toggleSubscriptionEvent(current, event);

    if (events === 0) return;

    this.setPending(subscription.id, events);

    this.commands
      .setEvents(subscription.scope, subscription.scopeEntityId, events)
      .subscribe((wasSaved) => {
        // A saved change stays pending until the refreshed list carries it, or the chip would
        // flick back for the length of the reload.
        if (!wasSaved) this.setPending(subscription.id, null);
      });
  }

  protected onRemove(subscription: NotificationSubscription) {
    this.commands.unsubscribe(subscription).subscribe();
  }

  private setPending(id: number, events: number | null) {
    this.pendingEvents.update((current) => {
      const next = new Map(current);

      if (events === null) {
        next.delete(id);
      } else {
        next.set(id, events);
      }

      return next;
    });
  }

  private toRow(
    subscription: NotificationSubscription,
    events: number
  ): SubscriptionRow {
    const name = subscription.name;

    return {
      subscription,
      scopeLabel: scopeLabel(subscription.scope),
      removeLabel: $localize`:Accessible label for the button that stops following a place. NAME is the board, sprint or project name:Stop following ${name}:NAME:`,
      events: EVENT_LABELS.map(({ event, label }) => {
        const isSelected = hasSubscriptionEvent(events, event);

        return {
          event,
          label,
          isSelected,
          isLocked: isSelected && events === event,
        };
      }),
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
