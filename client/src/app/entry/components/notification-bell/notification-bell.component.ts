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
import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { TemplatePortal } from '@angular/cdk/portal';
import { Router } from '@angular/router';
import { SpinnerComponent } from '@app/static/components/spinner/spinner.component';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { activityTypeToString } from '@core/transforms/activity-type';
import { fromNow } from '@core/util/dates';
import * as notificationActions from '@core/store/notifications/notifications.actions';
import {
  selectNotifications,
  selectNotificationsLoaded,
  selectUnreadCount,
} from '@core/store/notifications/notifications.selectors';
import { LucideBell } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { StrokedButtonComponent } from '@app/static/components/button/stroked-button.component';
import { NotificationViewModel } from '@core/models/view-models/notification-view-model';

@Component({
  selector: 'app-notification-bell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    StrokedButtonComponent,
    TooltipDirective,
    LucideBell,
    AvatarComponent,
    SpinnerComponent,
  ],
  template: `
    <button
      app-stroked-button
      appTooltip="Notifications"
      class="board-filter-button relative"
      (click)="toggleMenu()">
      <svg lucideBell aria-hidden="false" aria-label="Notifications"></svg>
      @if (unreadCount() > 0) {
        <span
          class="bg-primary text-primary-foreground absolute -top-1 -right-1 flex h-4 w-4 items-center justify-center rounded-full text-[10px] font-bold">
          {{ unreadCount() > 9 ? '9+' : unreadCount() }}
        </span>
      }
    </button>

    <ng-template #menuTemplate>
      <div
        class="custom-scroll border-border bg-background mt-[0.4rem] max-h-[80vh] max-w-120 min-w-100 overflow-y-auto rounded-[0.4rem] border p-1 py-3 shadow-lg">
        <div class="flex items-center justify-between px-[1.2rem] pb-2">
          <span class="text-sm font-semibold">Notifications</span>
          @if (unreadCount() > 0) {
            <button
              class="text-muted-foreground hover:text-foreground text-xs underline"
              (click)="markAllAsRead()">
              Mark all as read
            </button>
          }
        </div>

        <div class="border-border/50 mb-2 border-t"></div>

        @if (loaded()) {
          @for (notification of notifications(); track notification.id; let last = $last) {
            <div
              class="hover:bg-accent flex min-w-80 cursor-pointer flex-row items-center px-[1.2rem] py-[0.6rem] text-sm"
              [class.opacity-50]="notification.isRead"
              (click)="onNotificationClick(notification)">
              @if (!notification.isRead) {
                <span class="bg-primary mr-2 h-2 w-2 shrink-0 rounded-full"></span>
              } @else {
                <span class="mr-2 h-2 w-2 shrink-0"></span>
              }
              <app-avatar
                class="mr-2 shrink-0 grow-0 basis-8"
                [imageUrl]="notification.actorPictureUrl"
                [name]="notification.actorUsername"
                size="24">
              </app-avatar>
              <div class="flex flex-col">
                <span class="font-medium tracking-[0.225px]">
                  {{ notification.actorUsername }}
                </span>
                <span class="text-foreground/70 text-xs">
                  {{ activityTypeToString(notification.activityType) }} &bull;
                  {{ fromNow(notification.createdAt) }}
                </span>
              </div>
            </div>

            @if (!last) {
              <div class="border-border/50 my-1 w-full border-t"></div>
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
    </ng-template>
  `,
})
export class NotificationBellComponent implements OnDestroy {
  private store = inject(Store);
  private overlay = inject(Overlay);
  private vcr = inject(ViewContainerRef);
  private el = inject(ElementRef<HTMLElement>);
  private router = inject(Router);

  readonly notifications = this.store.selectSignal(selectNotifications);
  readonly unreadCount = this.store.selectSignal(selectUnreadCount);
  readonly loaded = this.store.selectSignal(selectNotificationsLoaded);

  readonly activityTypeToString = activityTypeToString;
  readonly fromNow = fromNow;

  private readonly menuTemplate = viewChild.required<TemplateRef<unknown>>('menuTemplate');
  private overlayRef?: OverlayRef;

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

  onNotificationClick(notification: NotificationViewModel) {
    if (!notification.isRead) {
      this.store.dispatch(notificationActions.markAsRead({ id: notification.id }));
    }

    if (notification.link) {
      void this.router.navigateByUrl(notification.link);
    }

    this.closeMenu();
  }

  markAllAsRead() {
    this.store.dispatch(notificationActions.markAllAsRead());
  }

  ngOnDestroy() {
    this.overlayRef?.dispose();
  }
}
