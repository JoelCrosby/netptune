import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { PageHeaderBackLinkComponent } from '@app/static/components/page-header/page-header-back-link.component';
import { Store } from '@ngrx/store';
import { ShellService } from './shell.service';
import { NotificationBellComponent } from '@app/entry/components/notification-bell/notification-bell.component';
import { CurrentSprintDropdownComponent } from './current-sprint-dropdown.component';

@Component({
  selector: 'app-shell-navbar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageHeaderBackLinkComponent,
    NotificationBellComponent,
    CurrentSprintDropdownComponent,
  ],
  template: `
    <div
      class="bg-background border-border sticky z-10 flex h-full items-center justify-between border-b px-4">
      <div class="h-6">
        <app-page-header-back-link />
      </div>

      <div class="ml-auto flex items-center justify-end gap-2 py-2">
        <app-current-sprint-dropdown />
        <app-notification-bell />
      </div>
    </div>
  `,
})
export class ShellNavbarComponent {
  readonly store = inject(Store);

  shell = inject(ShellService);
}
