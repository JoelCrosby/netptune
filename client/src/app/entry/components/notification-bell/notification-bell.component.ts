import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { TemplatePortal } from '@angular/cdk/portal';
import {
  Component,
  ElementRef,
  OnDestroy,
  TemplateRef,
  ViewContainerRef,
  inject,
  viewChild,
  computed,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { IconButtonComponent } from '@app/static/components/button/icon-button.component';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { selectIsAuthenticated } from '@core/store/auth/auth.selectors';
import {
  recentNotificationsResource,
  unreadNotificationCountResource,
} from '@core/resources/notification.resource';
import { NotificationCommandsService } from '@core/services/notification-commands.service';
import { LucideBell } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { NotificationDropdownComponent } from './notification-dropdown.component';

@Component({
  selector: 'app-notification-bell',
  imports: [
    IconButtonComponent,
    TooltipDirective,
    LucideBell,
    NotificationDropdownComponent,
  ],
  template: `
    <button
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
  `,
})
export class NotificationBellComponent implements OnDestroy {
  private store = inject(Store);
  private notificationCommands = inject(NotificationCommandsService);
  private overlay = inject(Overlay);
  private vcr = inject(ViewContainerRef);
  private el = inject(ElementRef<HTMLElement>);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  readonly authenticated = this.store.selectSignal(selectIsAuthenticated);
  private readonly recent = recentNotificationsResource();
  private readonly unread = unreadNotificationCountResource();

  readonly notifications = this.recent.value;
  readonly unreadCount = this.unread.value;
  readonly loaded = computed(() => !this.recent.isLoading());

  private readonly menuTemplate =
    viewChild.required<TemplateRef<unknown>>('menuTemplate');
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
