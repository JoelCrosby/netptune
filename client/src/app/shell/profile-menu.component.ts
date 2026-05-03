import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
} from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import {
  selectCurrentUser,
  selectHasPermission,
} from '@app/core/store/auth/auth.selectors';
import { logout } from '@core/store/auth/auth.actions';
import { netptunePermissions } from '@core/auth/permissions';
import { changeTheme } from '@core/store/settings/settings.actions';
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
  changeDetection: ChangeDetectionStrategy.OnPush,
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
        class="focus-visible:ring-primary focus-visible:ring-offset-background inline-flex h-9 w-9 cursor-pointer items-center justify-center rounded-full transition-opacity hover:opacity-85 focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none"
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
            {{ user.displayName || 'Profile' }}
          </div>
          @if (user.email) {
            <div class="text-muted-foreground max-w-48 truncate text-xs">
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
          Profile
        </button>

        @if (canReadWorkspace()) {
          <button
            app-menu-item
            type="button"
            (click)="navigateToWorkspaceSettings(profileMenu)">
            <svg lucideSettings class="h-4 w-4 shrink-0"></svg>
            Workspace settings
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
          Logout
        </button>
      </app-dropdown-menu>
    }
  `,
})
export class ProfileMenuComponent {
  private readonly store = inject(Store);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly user = this.store.selectSignal(selectCurrentUser);
  readonly effectiveTheme = this.store.selectSignal(selectEffectiveTheme);
  readonly canReadWorkspace = this.store.selectSignal(
    selectHasPermission(netptunePermissions.workspace.read)
  );

  readonly isDarkTheme = computed(() => this.effectiveTheme() === 'dark');
  readonly themeActionLabel = computed(() =>
    this.isDarkTheme() ? 'Use light theme' : 'Use dark theme'
  );
  readonly profileMenuLabel = computed(() => {
    const user = this.user();
    const name = user?.displayName || user?.email || 'user';

    return `Open ${name} menu`;
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
    this.store.dispatch(
      changeTheme({ theme: this.isDarkTheme() ? 'light' : 'dark' })
    );
  }

  logOut(menu: DropdownMenuComponent) {
    menu.close();
    this.store.dispatch(logout());
  }
}
