import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PageHeaderBackLinkComponent } from '@app/static/components/page-header/page-header-back-link.component';
import { selectIsAuthenticated } from '@core/store/auth/auth.selectors';
import { Store } from '@ngrx/store';
import { ButtonLinkComponent } from '@static/components/button/button-link.component';
import { ShellService } from './shell.service';
import { NotificationBellComponent } from '@app/entry/components/notification-bell/notification-bell.component';
import { CurrentSprintDropdownComponent } from './current-sprint-dropdown.component';
import { ProfileMenuComponent } from './profile-menu.component';
import { CommandPaletteButtonComponent } from './command-palette/command-palette-button.component';

@Component({
  selector: 'app-shell-navbar',
  imports: [
    PageHeaderBackLinkComponent,
    NotificationBellComponent,
    CurrentSprintDropdownComponent,
    ProfileMenuComponent,
    CommandPaletteButtonComponent,
    ButtonLinkComponent,
    RouterLink,
  ],
  template: `
    <div
      class="bg-background border-border sticky z-10 flex h-full items-center justify-between border-b px-4">
      <div class="h-6">
        <app-page-header-back-link />
      </div>

      <div class="ml-auto flex items-center justify-end gap-3 py-2">
        <app-current-sprint-dropdown />
        <app-command-palette-button />
        @if (authenticated()) {
          <app-notification-bell />
          <app-profile-menu />
        } @else {
          <a
            app-button-link
            variant="filled"
            routerLink="/auth/login"
            i18n="
              Navbar button that takes a signed-out visitor to the login page
            "
            >Sign in</a
          >
        }
      </div>
    </div>
  `,
})
export class ShellNavbarComponent {
  readonly store = inject(Store);

  shell = inject(ShellService);

  readonly authenticated = this.store.selectSignal(selectIsAuthenticated);
}
