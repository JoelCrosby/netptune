import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { selectIsAuthenticated } from '@core/auth/store/auth.selectors';
import { Workspace } from '@core/models/workspace';
import { toggleSideMenu } from '@core/store/layout/layout.actions';
import { selectSideMenuOpen } from '@core/store/layout/layout.selectors';
import { Store } from '@ngrx/store';
import { ShellSidebarComponent } from './shell-sidebar.component';
import { ShellService } from './shell.service';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ShellService],
  imports: [RouterOutlet, ShellSidebarComponent],
  template: `
    <div class="bg-background h-full">
      @if (sideMenuOpen()) {
        <app-shell-sidebar (workspaceChange)="onWorkspaceChange($event)" />
      }

      <main
        class="h-screen [transition:margin-left_.2s_ease-in-out]"
        [class.ml-[248px]]="shell.sideNavExpanded()"
        [class.ml-[72px]]="sideMenuOpen()">
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
