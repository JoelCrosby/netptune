import { Component, inject } from '@angular/core';
import { SessionService } from '@core/services/session.service';
import { RouterLink } from '@angular/router';
import { PageHeaderBackLinkComponent } from '@app/static/components/page-header/page-header-back-link.component';
import { ButtonLinkComponent } from '@static/components/button/button-link.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { LayoutService } from '@core/services/layout.service';
import { LucideMenu } from '@lucide/angular';
import { ShellService } from './shell.service';
import { NotificationBellComponent } from '@app/entry/components/notification-bell/notification-bell.component';
import { CurrentSprintDropdownComponent } from './current-sprint-dropdown.component';
import { ProfileMenuComponent } from './profile-menu.component';
import { CommandPaletteButtonComponent } from './command-palette/command-palette-button.component';
import { AiAssistantButtonComponent } from './ai-assistant/ai-assistant-button.component';

@Component({
  selector: 'app-shell-navbar',
  imports: [
    PageHeaderBackLinkComponent,
    NotificationBellComponent,
    CurrentSprintDropdownComponent,
    ProfileMenuComponent,
    CommandPaletteButtonComponent,
    AiAssistantButtonComponent,
    ButtonLinkComponent,
    IconButtonComponent,
    LucideMenu,
    RouterLink,
  ],
  template: `
    <div
      class="bg-background border-border sticky z-10 flex h-full items-center justify-between gap-2 border-b px-3 md:px-4">
      @if (shell.isDrawer()) {
        <button
          app-icon-button
          type="button"
          class="shrink-0"
          i18n-aria-label="
            Accessible label for the button that opens the sidebar on small
            screens
          "
          aria-label="Open Menu"
          aria-haspopup="dialog"
          [attr.aria-expanded]="layout.sideMenuOpen()"
          (click)="layout.openSideMenu()">
          <svg lucideMenu></svg>
        </button>
      }

      <div class="h-6 min-w-0">
        <app-page-header-back-link />
      </div>

      <div class="ml-auto flex items-center justify-end gap-2 py-2 md:gap-3">
        <app-current-sprint-dropdown class="hidden md:block" />
        <app-command-palette-button />
        @if (authenticated()) {
          <app-ai-assistant-button />
          <app-notification-bell />
          <app-profile-menu />
        } @else {
          <a
            app-button-link
            variant="filled"
            routerLink="/auth/login"
            i18n="
              Navbar button that takes a signed-out visitor to the login page
            ">
            Sign in
          </a>
        }
      </div>
    </div>
  `,
})
export class ShellNavbarComponent {
  shell = inject(ShellService);
  layout = inject(LayoutService);

  readonly authenticated = inject(SessionService).isAuthenticated;
}
