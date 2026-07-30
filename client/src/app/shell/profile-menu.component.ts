import { Component, computed, inject } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import {
  selectCurrentUser,
  selectHasPermission,
} from '@app/core/store/auth/auth.selectors';
import { logout } from '@core/store/auth/auth.actions';
import { netptunePermissions } from '@core/auth/permissions';
import { APPEARANCE_THEME } from '@core/models/user-preferences';
import { UserPreferencesService } from '@core/services/user-preferences.service';
import { selectEffectiveTheme } from '@core/store/settings/settings.selectors';
import {
  LucideLogOut,
  LucideMoon,
  LucideSettings,
  LucideSun,
  LucideUser,
} from '@lucide/angular';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';

@Component({
  selector: 'app-profile-menu',
  imports: [
    AvatarComponent,
    DropdownMenuComponent,
    MenuItemComponent,
    LucideLogOut,
    LucideMoon,
    LucideSettings,
    LucideSun,
    LucideUser,
  ],
  template: `
    @if (user(); as user) {
      <button
        #profileTrigger
        type="button"
        class="focus-visible:ring-primary focus-visible:ring-offset-background block h-9 w-9 cursor-pointer items-center justify-center rounded-full transition-opacity hover:opacity-85 focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none"
        aria-haspopup="menu"
        [attr.aria-label]="profileMenuLabel()"
        (click)="profileMenu.toggle(profileTrigger)">
        <app-avatar
          [name]="user.displayName"
          [imageUrl]="user.pictureUrl"
          size="md"
          [border]="true"
          [tooltip]="false" />
      </button>

      <app-dropdown-menu #profileMenu xPosition="before">
        <div class="min-w-56 px-3 py-2">
          <div class="max-w-48 truncate text-sm font-semibold">
            {{ user.displayName || profileFallbackName }}
          </div>
          @if (user.email) {
            <div class="text-muted max-w-48 truncate text-xs">
              {{ user.email }}
            </div>
          }
        </div>

        <div class="border-border/50 my-1 border-t"></div>

        <button
          app-menu-item
          type="button"
          (click)="navigateToProfile(profileMenu)">
          <svg lucideUser class="h-4 w-4 shrink-0"></svg>
          <span i18n="Profile menu item that opens the signed-in user's profile"
            >Profile</span
          >
        </button>

        @if (canReadWorkspace()) {
          <button
            app-menu-item
            type="button"
            (click)="navigateToWorkspaceSettings(profileMenu)">
            <svg lucideSettings class="h-4 w-4 shrink-0"></svg>
            <span i18n="Profile menu item that opens workspace settings"
              >Workspace settings</span
            >
          </button>
        }

        <button app-menu-item type="button" (click)="toggleTheme(profileMenu)">
          @if (isDarkTheme()) {
            <svg lucideSun class="h-4 w-4 shrink-0"></svg>
          } @else {
            <svg lucideMoon class="h-4 w-4 shrink-0"></svg>
          }
          {{ themeActionLabel() }}
        </button>

        <div class="border-border/50 my-1 border-t"></div>

        <button app-menu-item type="button" (click)="logOut(profileMenu)">
          <svg lucideLogOut class="h-4 w-4 shrink-0"></svg>
          <span i18n="Profile menu item that signs the user out">Logout</span>
        </button>
      </app-dropdown-menu>
    }
  `,
})
export class ProfileMenuComponent {
  private readonly store = inject(Store);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly preferences = inject(UserPreferencesService);

  readonly user = this.store.selectSignal(selectCurrentUser);
  readonly effectiveTheme = this.store.selectSignal(selectEffectiveTheme);
  readonly canReadWorkspace = this.store.selectSignal(
    selectHasPermission(netptunePermissions.workspace.read)
  );

  readonly profileFallbackName = $localize`:profile menu heading|Heading of the profile menu when the account has no display name:Profile`;

  readonly isDarkTheme = computed(() => this.effectiveTheme() === 'dark');
  readonly themeActionLabel = computed(() => {
    return this.isDarkTheme()
      ? $localize`:Profile menu item that switches from the dark theme to the light one:Use light theme`
      : $localize`:Profile menu item that switches from the light theme to the dark one:Use dark theme`;
  });
  readonly profileMenuLabel = computed(() => {
    const user = this.user();
    const name =
      user?.displayName ||
      user?.email ||
      $localize`:Stands in for the user's name in the profile menu label when the account has neither a display name nor an e-mail address:user`;

    return $localize`:Accessible label for the button that opens the profile menu. USER_NAME is the display name, e-mail address, or a generic fallback:Open ${name}:USER_NAME: menu`;
  });

  navigateToProfile(menu: DropdownMenuComponent) {
    menu.close();
    void this.router.navigate(['./profile'], { relativeTo: this.route });
  }

  navigateToWorkspaceSettings(menu: DropdownMenuComponent) {
    menu.close();
    void this.router.navigate(['./settings'], { relativeTo: this.route });
  }

  toggleTheme(menu: DropdownMenuComponent) {
    menu.close();
    this.preferences
      .updateValue(
        APPEARANCE_THEME,
        'global',
        this.isDarkTheme() ? 'light' : 'dark'
      )
      .subscribe();
  }

  logOut(menu: DropdownMenuComponent) {
    menu.close();
    this.store.dispatch(logout());
  }
}
