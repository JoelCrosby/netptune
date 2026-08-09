import { Component, computed, inject, signal } from '@angular/core';
import { SessionService } from '@core/services/session.service';
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
import { Workspace } from '@core/models/workspace';
import { LayoutService } from '@core/services/layout.service';
import { ShellSidebarComponent } from './shell-sidebar.component';
import { ShellService } from './shell.service';
import { ShellNavbarComponent } from './shell-navbar.component';
import { AiAssistantComponent } from './ai-assistant/ai-assistant.component';
import { AiAssistantPanelComponent } from './ai-assistant/ai-assistant-panel.component';
import { AiAssistantService } from '@core/services/ai-assistant.service';
import { AiPanelService } from '@core/services/ai-panel.service';
import { CommandPaletteComponent } from './command-palette/command-palette.component';
import { GlobalCommandsService } from './global-commands.service';
import { LastWorkspaceService } from '@core/services/last-workspace.service';
import { UserPreferencesService } from '@core/services/user-preferences.service';
import { CommandShortcutService } from './command-palette/command-shortcut.service';

@Component({
  providers: [ShellService, GlobalCommandsService, CommandShortcutService],
  imports: [
    RouterOutlet,
    ShellSidebarComponent,
    ShellNavbarComponent,
    CommandPaletteComponent,
    AiAssistantComponent,
    AiAssistantPanelComponent,
  ],
  styles: `
    .expanded {
      grid-template-columns: 247px auto;
    }
    .collapsed {
      grid-template-columns: 72px auto;
    }
    .expanded.docked {
      grid-template-columns: 247px auto var(--assistant-dock-width);
    }
    .collapsed.docked {
      grid-template-columns: 72px auto var(--assistant-dock-width);
    }

    @keyframes assistant-dock-in {
      from {
        opacity: 0;
        clip-path: inset(0 0 0 100%);
      }
      to {
        opacity: 1;
        clip-path: inset(0 0 0 0);
      }
    }

    .assistant-dock {
      animation: assistant-dock-in 180ms ease-out;
    }

    @media (prefers-reduced-motion: reduce) {
      .assistant-dock {
        animation: none;
      }
    }
  `,
  template: `
    @if (chunkLoading()) {
      <div
        class="bg-primary/20 fixed inset-x-0 top-0 z-50 h-0.5 overflow-hidden">
        <div class="bg-primary animate-loading-bar h-full w-1/3"></div>
      </div>
    }
    <div
      class="bg-background fixed grid h-screen w-screen grid-rows-[60px_minmax(0,1fr)] transition-all"
      [class.expanded]="shell.sideNavExpanded()"
      [class.collapsed]="shell.sideNavCollapsed()"
      [class.docked]="panel.isDocked()"
      [style.--assistant-dock-width]="dockWidth()"
      [style.transition]="panel.isResizing() ? 'none' : null">
      @if (sideMenuOpen()) {
        <app-shell-sidebar
          class="col-start-1 row-span-2 row-start-1"
          (workspaceChange)="onWorkspaceChange($event)" />
      }
      <app-shell-navbar />

      <main
        class="col-start-2 row-start-2 scrollbar-gutter-stable overflow-y-auto">
        <router-outlet />
      </main>

      @if (panel.isDocked()) {
        <app-ai-assistant-panel
          class="assistant-dock border-border col-start-3 row-span-2 row-start-1 border-l" />
      }
    </div>

    <app-command-palette></app-command-palette>
    <app-ai-assistant></app-ai-assistant>
  `,
})
export class ShellComponent {
  private router = inject(Router);

  private layout = inject(LayoutService);

  shell = inject(ShellService);
  readonly panel = inject(AiPanelService);
  /** Held so the chat restores its workspace session for as long as the shell is up. */
  readonly assistant = inject(AiAssistantService);
  readonly globalCommands = inject(GlobalCommandsService);
  readonly commandShortcuts = inject(CommandShortcutService);
  readonly preferences = inject(UserPreferencesService);
  readonly lastWorkspace = inject(LastWorkspaceService);

  authenticated = inject(SessionService).isAuthenticated;
  sideMenuOpen = this.layout.sideMenuOpen;

  readonly chunkLoading = signal(false);

  readonly dockWidth = computed(() => {
    return `min(${this.panel.width()}px, 50vw)`;
  });

  constructor() {
    if (this.authenticated()) {
      this.preferences.load();
    }

    this.router.events
      .pipe(
        filter(
          (e) =>
            e instanceof RouteConfigLoadStart ||
            e instanceof RouteConfigLoadEnd ||
            e instanceof NavigationCancel ||
            e instanceof NavigationError
        ),
        takeUntilDestroyed()
      )
      .subscribe((e) =>
        this.chunkLoading.set(e instanceof RouteConfigLoadStart)
      );
  }

  onSidenavClosedStart() {
    this.layout.toggleSideMenu();
  }

  onWorkspaceChange(workspace: Workspace) {
    if (!workspace) {
      throw new Error('onWorkspaceChange workspace is null');
    }

    void this.router.navigate(['/', workspace.slug]);
  }
}
