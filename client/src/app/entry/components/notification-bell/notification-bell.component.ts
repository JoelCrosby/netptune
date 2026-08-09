import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { TemplatePortal } from '@angular/cdk/portal';
import {
  Component,
  ElementRef,
  OnDestroy,
  TemplateRef,
  ViewContainerRef,
  effect,
  inject,
  signal,
  untracked,
  viewChild,
  computed,
} from '@angular/core';
import { SessionService } from '@core/services/session.service';
import { ActivatedRoute, Router } from '@angular/router';
import { IconButtonComponent } from '@app/static/components/button/icon-button.component';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { NotificationViewModel } from '@core/models/view-models/notification-view-model';
import {
  recentNotificationsResource,
  unreadNotificationCountResource,
} from '@core/resources/notification.resource';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { NotificationCommandsService } from '@core/services/notification-commands.service';
import { LucideBell } from '@lucide/angular';
import { anchoredPopup } from '@static/components/anchored-popup/anchored-popup';
import { NotificationDropdownComponent } from './notification-dropdown.component';
import { NotificationPopupComponent } from './notification-popup.component';

const POPUP_TIMEOUT = 12000;

@Component({
  selector: 'app-notification-bell',
  imports: [
    IconButtonComponent,
    TooltipDirective,
    LucideBell,
    NotificationDropdownComponent,
    NotificationPopupComponent,
  ],
  template: `
    <button
      #trigger
      app-icon-button
      i18n-aria-label="Accessible label for the notifications button"
      aria-label="Notifications"
      i18n-appTooltip="Tooltip on the notifications button"
      appTooltip="Notifications"
      appTooltipPosition="bottom"
      class="text-foreground/80 relative mr-2"
      (click)="toggleMenu()">
      <svg lucideBell aria-hidden="true"></svg>
      @if (unreadCount() > 0) {
        <span
          class="bg-primary absolute -top-1 -right-1 flex h-4 w-4 items-center justify-center rounded-full text-[10px] font-bold text-white dark:text-black">
          {{ unreadCount() > 9 ? '9+' : unreadCount() }}
        </span>
      }
    </button>

    <ng-template #menuTemplate>
      <app-notification-dropdown
        [notifications]="notifications()"
        [unreadCount]="unreadCount()"
        [loaded]="loaded()"
        (markAllAsRead)="markAllAsRead()"
        (viewAll)="onViewAll()" />
    </ng-template>

    <ng-template #popupTemplate>
      @if (arrived(); as notification) {
        <app-notification-popup
          [notification]="notification"
          (opened)="openArrived(notification)"
          (dismissed)="dismissPopup()" />
      }
    </ng-template>
  `,
})
export class NotificationBellComponent implements OnDestroy {
  private notificationCommands = inject(NotificationCommandsService);
  private overlay = inject(Overlay);
  private vcr = inject(ViewContainerRef);
  private el = inject(ElementRef<HTMLElement>);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  readonly authenticated = inject(SessionService).isAuthenticated;
  private readonly recent = recentNotificationsResource();
  private readonly unread = unreadNotificationCountResource();
  private readonly workspaceId = inject(CurrentWorkspaceService).id;

  readonly notifications = this.recent.value;
  readonly unreadCount = this.unread.value;
  readonly loaded = computed(() => !this.recent.isLoading());

  private readonly trigger = viewChild('trigger', { read: ElementRef });
  private readonly menuTemplate =
    viewChild.required<TemplateRef<unknown>>('menuTemplate');
  private readonly popupTemplate =
    viewChild.required<TemplateRef<unknown>>('popupTemplate');
  private overlayRef?: OverlayRef;

  protected readonly arrived = signal<NotificationViewModel | null>(null);

  private readonly popup = anchoredPopup({ timeout: POPUP_TIMEOUT });

  private highestSeenId: number | null = null;
  private seenWorkspaceId: number | undefined;

  constructor() {
    effect(() => {
      const notifications = this.notifications();
      const workspaceId = this.workspaceId();

      if (this.recent.status() !== 'resolved') return;

      untracked(() => this.onNotificationsResolved(notifications, workspaceId));
    });
  }

  private onNotificationsResolved(
    notifications: NotificationViewModel[],
    workspaceId: number | undefined
  ) {
    const highestId = notifications.reduce((highest, notification) => {
      return Math.max(highest, notification.id);
    }, 0);

    if (this.highestSeenId === null || this.seenWorkspaceId !== workspaceId) {
      this.highestSeenId = highestId;
      this.seenWorkspaceId = workspaceId;

      return;
    }

    const previousHighestId = this.highestSeenId;

    this.highestSeenId = Math.max(previousHighestId, highestId);

    if (this.overlayRef?.hasAttached()) return;

    const newest = notifications
      .filter(
        (notification) =>
          notification.id > previousHighestId && !notification.isRead
      )
      .sort((left, right) => right.id - left.id)
      .at(0);

    const trigger = this.trigger();

    if (!newest || !trigger) return;

    this.arrived.set(newest);
    this.popup.show(trigger, this.popupTemplate());
  }

  protected openArrived(notification: NotificationViewModel) {
    this.dismissPopup();

    if (!notification.isRead) {
      this.notificationCommands.markAsRead(notification.id);
    }

    void this.router.navigateByUrl(notification.link);
  }

  protected dismissPopup() {
    this.popup.hide();
    this.arrived.set(null);
  }

  toggleMenu() {
    if (this.overlayRef?.hasAttached()) {
      this.closeMenu();
    } else {
      this.openMenu();
    }
  }

  private openMenu() {
    this.dismissPopup();

    const el = this.el.nativeElement.querySelector('button') as HTMLElement;
    const positionStrategy = this.overlay
      .position()
      .flexibleConnectedTo(el)
      .withPositions([
        {
          originX: 'start',
          originY: 'bottom',
          overlayX: 'start',
          overlayY: 'top',
        },
        {
          originX: 'start',
          originY: 'top',
          overlayX: 'start',
          overlayY: 'bottom',
        },
      ]);

    this.overlayRef = this.overlay.create({
      positionStrategy,
      hasBackdrop: true,
      backdropClass: 'cdk-overlay-transparent-backdrop',
      scrollStrategy: this.overlay.scrollStrategies.reposition(),
    });

    this.overlayRef.attach(new TemplatePortal(this.menuTemplate(), this.vcr));
    this.overlayRef.backdropClick().subscribe(() => this.closeMenu());

    this.recent.reload();
  }

  private closeMenu() {
    this.overlayRef?.detach();
  }

  markAllAsRead() {
    this.notificationCommands.markAllAsRead();
  }

  onViewAll() {
    this.closeMenu();
    void this.router.navigate(['notifications'], { relativeTo: this.route });
  }

  ngOnDestroy() {
    this.overlayRef?.dispose();
  }
}
