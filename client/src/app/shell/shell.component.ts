import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  NavigationCancel,
  NavigationError,
  RouteConfigLoadEnd,
  RouteConfigLoadStart,
  Router,
  RouterOutlet,
} from '@angular/router';
import { filter } from 'rxjs';
import { selectIsAuthenticated } from '@app/core/store/auth/auth.selectors';
import { Workspace } from '@core/models/workspace';
import { toggleSideMenu } from '@core/store/layout/layout.actions';
import { selectSideMenuOpen } from '@core/store/layout/layout.selectors';
import { Store } from '@ngrx/store';
import { ShellSidebarComponent } from './shell-sidebar.component';
import { ShellService } from './shell.service';
import { ShellNavbarComponent } from './shell-navbar.component';
import { CommandPaletteComponent } from './command-palette/command-palette.component';
import { GlobalCommandsService } from './global-commands.service';
import { UserPreferencesService } from '@core/services/user-preferences.service';

@Component({
  providers: [ShellService, GlobalCommandsService],
  imports: [
    RouterOutlet,
    ShellSidebarComponent,
    ShellNavbarComponent,
    CommandPaletteComponent,
  ],
  styles: `
    .expanded {
      grid-template-columns: 247px auto;
    }
    .collapsed {
      grid-template-columns: 72px auto;
    }
  `,
  template: `
    @if (chunkLoading()) {
      <div class="bg-primary/20 fixed inset-x-0 top-0 z-50 h-0.5 overflow-hidden">
        <div class="bg-primary animate-loading-bar h-full w-1/3"></div>
      </div>
    }
    <div
      class="bg-background fixed grid h-screen w-screen grid-rows-[60px_auto] transition-all"
      [class.expanded]="shell.sideNavExpanded()"
      [class.collapsed]="shell.sideNavCollapsed()">
      @if (sideMenuOpen()) {
        <app-shell-sidebar
          class="col-start-1 row-span-2 row-start-1"
          (workspaceChange)="onWorkspaceChange($event)" />
      }
      <app-shell-navbar />

      <main class="col-start-2 row-start-2 overflow-y-auto">
        <router-outlet />
      </main>
    </div>

    <app-command-palette></app-command-palette>
  `,
})
export class ShellComponent {
  private store = inject(Store);
  private router = inject(Router);

  shell = inject(ShellService);
  readonly globalCommands = inject(GlobalCommandsService);
  readonly preferences = inject(UserPreferencesService);
  authenticated = this.store.selectSignal(selectIsAuthenticated);
  sideMenuOpen = this.store.selectSignal(selectSideMenuOpen);

  readonly chunkLoading = signal(false);

  constructor() {
    this.preferences.load();

    this.router.events
      .pipe(
        filter(
          (e) =>
            e instanceof RouteConfigLoadStart ||
            e instanceof RouteConfigLoadEnd ||
            e instanceof NavigationCancel ||
            e instanceof NavigationError,
        ),
        takeUntilDestroyed(),
      )
      .subscribe((e) => this.chunkLoading.set(e instanceof RouteConfigLoadStart));
  }

  onSidenavClosedStart() {
    this.store.dispatch(toggleSideMenu());
  }

  onWorkspaceChange(workspace: Workspace) {
    if (!workspace) {
      throw new Error('onWorkspaceChange workspace is null');
    }

    void this.router.navigate(['/', workspace.slug]);
  }
}
