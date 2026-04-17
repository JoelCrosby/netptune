import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { selectIsAuthenticated } from '@core/auth/store/auth.selectors';
import { Workspace } from '@core/models/workspace';
import { toggleSideMenu } from '@core/store/layout/layout.actions';
import { selectSideMenuOpen } from '@core/store/layout/layout.selectors';
import { Store } from '@ngrx/store';
import { ShellSidebarComponent } from './shell-sidebar.component';
import { ShellService } from './shell.service';
import { ShellNavbarComponent } from './shell-navbar.component';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ShellService],
  imports: [RouterOutlet, ShellSidebarComponent, ShellNavbarComponent],
  template: `
    <div
      class="bg-background grid h-screen w-screen grid-cols-[247px_auto] grid-rows-[60px_auto] gap-[1px]">
      @if (sideMenuOpen()) {
        <app-shell-sidebar
          class="col-start-1 row-span-2 row-start-1"
          (workspaceChange)="onWorkspaceChange($event)" />
      }
      <app-shell-navbar />

      <main class="col-start-2 row-start-2 overflow-auto">
        <router-outlet />
      </main>
    </div>
  `,
})
export class ShellComponent {
  private store = inject(Store);
  private router = inject(Router);

  shell = inject(ShellService);
  authenticated = this.store.selectSignal(selectIsAuthenticated);
  sideMenuOpen = this.store.selectSignal(selectSideMenuOpen);

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
