import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { PageHeaderBackLinkComponent } from '@app/static/components/page-header/page-header-back-link.component';
import { Store } from '@ngrx/store';
import { ShellService } from './shell.service';
import { NotificationBellComponent } from '@app/entry/components/notification-bell/notification-bell.component';

@Component({
  selector: 'app-shell-navbar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PageHeaderBackLinkComponent, NotificationBellComponent],
  template: `
    <div
      class="w-inherit bg-background border-border fixed top-0 z-10 flex h-15 items-center justify-between border-b px-4">
      <div class="h-6">
        <app-page-header-back-link />
      </div>

      <div class="flex justify-center py-2">
        <app-notification-bell />
      </div>
    </div>
  `,
})
export class ShellNavbarComponent {
  readonly store = inject(Store);

  shell = inject(ShellService);
}
