import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { TemplatePortal } from '@angular/cdk/portal';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  TemplateRef,
  ViewContainerRef,
  inject,
  viewChild,
} from '@angular/core';
import { IconButtonComponent } from '@app/static/components/button/icon-button.component';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import * as notificationActions from '@core/store/notifications/notifications.actions';
import { loadNotifications } from '@core/store/notifications/notifications.actions';
import {
  selectNotifications,
  selectNotificationsLoaded,
  selectUnreadCount,
} from '@core/store/notifications/notifications.selectors';
import { LucideBell } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { NotificationDropdownComponent } from './notification-dropdown.component';

@Component({
  selector: 'app-notification-bell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    IconButtonComponent,
    TooltipDirective,
    LucideBell,
    NotificationDropdownComponent,
  ],
  template: `
    <button
      app-icon-button
      appTooltip="Notifications"
      appTooltipPosition="bottom"
      class="text-foreground/80 relative mr-2"
      (click)="toggleMenu()">
      <svg lucideBell aria-hidden="false" aria-label="Notifications"></svg>
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
        (markAllAsRead)="markAllAsRead()" />
    </ng-template>
  `,
})
export class NotificationBellComponent implements OnDestroy {
  private store = inject(Store);
  private overlay = inject(Overlay);
  private vcr = inject(ViewContainerRef);
  private el = inject(ElementRef<HTMLElement>);

  readonly notifications = this.store.selectSignal(selectNotifications);
  readonly unreadCount = this.store.selectSignal(selectUnreadCount);
  readonly loaded = this.store.selectSignal(selectNotificationsLoaded);

  private readonly menuTemplate =
    viewChild.required<TemplateRef<unknown>>('menuTemplate');
  private overlayRef?: OverlayRef;

  constructor() {
    this.store.dispatch(loadNotifications());
  }

  toggleMenu() {
    if (this.overlayRef?.hasAttached()) {
      this.closeMenu();
    } else {
      this.openMenu();
    }
  }

  private openMenu() {
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

    this.store.dispatch(notificationActions.loadNotifications());
  }

  private closeMenu() {
    this.overlayRef?.detach();
  }

  markAllAsRead() {
    this.store.dispatch(notificationActions.markAllAsRead());
  }

  ngOnDestroy() {
    this.overlayRef?.dispose();
  }
}
