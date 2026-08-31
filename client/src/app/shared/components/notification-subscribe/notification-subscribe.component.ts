import {
  Component,
  ElementRef,
  booleanAttribute,
  computed,
  inject,
  input,
  linkedSignal,
} from '@angular/core';
import {
  NotificationScope,
  NotificationSubscriptionEvent,
  hasSubscriptionEvent,
  toggleSubscriptionEvent,
} from '@core/models/notification-subscription';
import { NotificationSubscriptionCommandsService } from '@core/services/notification-subscription-commands.service';
import { NotificationSubscriptionsService } from '@core/services/notification-subscriptions.service';
import { LucideBell, LucideBellRing } from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuCheckboxItemComponent } from '@static/components/dropdown-menu/menu-checkbox-item.component';
import { FilterActionButtonComponent } from '@static/components/filter-action-button/filter-action-button.component';

interface EventOption {
  event: NotificationSubscriptionEvent;
  label: string;
}

const EVENT_OPTIONS: EventOption[] = [
  {
    event: NotificationSubscriptionEvent.taskCreated,
    label: $localize`:Notification subscription event, a task was created here:Tasks created`,
  },
  {
    event: NotificationSubscriptionEvent.taskUpdated,
    label: $localize`:Notification subscription event, a task here changed:Tasks updated`,
  },
  {
    event: NotificationSubscriptionEvent.taskAdded,
    label: $localize`:Notification subscription event, a task joined this place:Tasks added`,
  },
  {
    event: NotificationSubscriptionEvent.taskRemoved,
    label: $localize`:Notification subscription event, a task left this place:Tasks removed`,
  },
];

@Component({
  selector: 'app-notification-subscribe',
  imports: [
    DropdownMenuComponent,
    FilterActionButtonComponent,
    IconButtonComponent,
    LucideBell,
    LucideBellRing,
    MenuCheckboxItemComponent,
  ],
  // No `display: contents` here: the dropdown positions itself against this host's
  // getBoundingClientRect, and an element generating no box measures as a zero rect at the
  // top-left of the viewport.
  template: `
    @if (appearance() === 'toolbar') {
      <app-filter-action-button
        [label]="buttonLabel()"
        [icon]="isSubscribed() ? bellRingIcon : bellIcon"
        [color]="isSubscribed() ? 'primary' : undefined"
        [dot]="isSubscribed()"
        (action)="menu.toggle(el.nativeElement)" />
    } @else {
      <button
        app-icon-button
        type="button"
        [class]="iconButtonClass()"
        [title]="buttonLabel()"
        [attr.aria-label]="buttonLabel()"
        (click)="menu.toggle(el.nativeElement)">
        @if (isSubscribed()) {
          <svg lucideBellRing class="text-primary h-4 w-4"></svg>
        } @else {
          <svg lucideBell class="h-4 w-4"></svg>
        }
      </button>
    }

    <app-dropdown-menu #menu>
      <div class="min-w-56">
        <p
          class="text-muted px-3 py-2 text-xs font-medium tracking-wide uppercase"
          i18n="Heading above the per-scope notification event toggles">
          Notify me about
        </p>

        @for (option of eventOptions; track option.event) {
          <button
            app-menu-checkbox-item
            [checked]="isSelected(option.event)"
            (checkedChange)="onEventToggled(option.event)">
            <span class="flex-1">{{ option.label }}</span>
          </button>
        }

        <p
          class="text-muted border-border mt-1 border-t px-3 py-2 text-xs"
          i18n="
            Explains that per-scope notifications still obey the personal
            notification settings
          ">
          Your notification settings still decide how these reach you.
        </p>
      </div>
    </app-dropdown-menu>
  `,
})
export class NotificationSubscribeComponent {
  readonly scope = input.required<NotificationScope>();
  readonly scopeEntityId = input.required<number>();
  readonly scopeName = input<string>();
  readonly appearance = input<'toolbar' | 'icon'>('icon');

  // Board columns carry one of these each, so an unsubscribed bell stays out of the way until the
  // column is hovered. A subscribed one always shows, or the setting would be invisible.
  readonly revealOnHover = input(false, { transform: booleanAttribute });

  readonly el = inject(ElementRef);

  private readonly commands = inject(NotificationSubscriptionCommandsService);
  private readonly subscriptions = inject(NotificationSubscriptionsService);

  protected readonly bellIcon = LucideBell;
  protected readonly bellRingIcon = LucideBellRing;
  protected readonly eventOptions = EVENT_OPTIONS;

  private readonly subscription = computed(() => {
    return this.subscriptions.find(this.scope(), this.scopeEntityId());
  });

  protected readonly events = linkedSignal(
    () => this.subscription()?.events ?? 0
  );

  protected readonly isSubscribed = computed(() => this.events() !== 0);

  protected readonly iconButtonClass = computed(() => {
    const isHidden = this.revealOnHover() && !this.isSubscribed();

    return isHidden ? 'invisible group-hover/header:visible' : '';
  });

  protected readonly buttonLabel = computed(() => {
    const name = this.scopeName();

    if (this.isSubscribed()) {
      return name
        ? $localize`:Tooltip on the notify control when already subscribed. NAME is the board, sprint or project name:You are being notified about ${name}:NAME:`
        : $localize`:Tooltip on the notify control when already subscribed:You are being notified about this`;
    }

    return name
      ? $localize`:Tooltip on the control that subscribes to activity. NAME is the board, sprint or project name:Notify me about ${name}:NAME:`
      : $localize`:Tooltip on the control that subscribes to activity:Notify me about this`;
  });

  protected isSelected(event: NotificationSubscriptionEvent): boolean {
    return hasSubscriptionEvent(this.events(), event);
  }

  protected onEventToggled(event: NotificationSubscriptionEvent) {
    const previous = this.events();
    const events = toggleSubscriptionEvent(previous, event);

    this.events.set(events);

    const subscription = this.subscription();
    const isUnsubscribing = events === 0 && subscription !== undefined;
    const command = isUnsubscribing
      ? this.commands.unsubscribe(subscription)
      : this.commands.setEvents(this.scope(), this.scopeEntityId(), events);

    // A reload cannot undo this on its own: the linked signal only resets when the value it derives
    // from changes, and a rejected save leaves the stored events exactly as they were.
    command.subscribe((wasSaved) => {
      if (!wasSaved) this.events.set(previous);
    });
  }
}
