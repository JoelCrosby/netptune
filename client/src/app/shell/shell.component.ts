import { CdkTrapFocus } from '@angular/cdk/a11y';
import {
  Component,
  computed,
  inject,
  linkedSignal,
  signal,
} from '@angular/core';
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
import { BoardBackgroundService } from '@core/services/board-background.service';
import { LastWorkspaceService } from '@core/services/last-workspace.service';
import { UserPreferencesService } from '@core/services/user-preferences.service';
import { CommandShortcutService } from './command-palette/command-shortcut.service';
import { animatedPresence } from '@core/util/animated-presence';

const DOCK_ANIMATION_MS = 180;
const DRAWER_ANIMATION_MS = 200;

@Component({
  providers: [ShellService, GlobalCommandsService, CommandShortcutService],
  imports: [
    CdkTrapFocus,
    RouterOutlet,
    ShellSidebarComponent,
    ShellNavbarComponent,
    CommandPaletteComponent,
    AiAssistantComponent,
    AiAssistantPanelComponent,
  ],
  styles: `
    /* The dock track is always present so the column widths can interpolate
       between the closed and open layouts instead of snapping. */
    .shell-grid {
      transition: grid-template-columns 180ms ease-out;
    }
    .expanded {
      grid-template-columns: 247px auto 0px;
    }
    .collapsed {
      grid-template-columns: 72px auto 0px;
    }
    .expanded.docked {
      grid-template-columns: 247px auto var(--assistant-dock-width);
    }
    .collapsed.docked {
      grid-template-columns: 72px auto var(--assistant-dock-width);
    }
    .drawer {
      grid-template-columns: 0px minmax(0, 1fr) 0px;
    }

    .shell-grid,
    .shell-drawer {
      padding-top: env(safe-area-inset-top);
      padding-bottom: env(safe-area-inset-bottom);
      padding-left: env(safe-area-inset-left);
    }
    .shell-grid {
      padding-right: env(safe-area-inset-right);
    }

    @keyframes drawer-in {
      from {
        transform: translateX(-100%);
      }
    }
    @keyframes drawer-out {
      to {
        transform: translateX(-100%);
      }
    }
    @keyframes backdrop-in {
      from {
        opacity: 0;
      }
    }
    @keyframes backdrop-out {
      to {
        opacity: 0;
      }
    }

    .shell-drawer {
      animation: drawer-in 200ms ease-out;
    }
    .shell-drawer-leaving {
      animation: drawer-out 200ms ease-in forwards;
      pointer-events: none;
    }
    .shell-drawer-backdrop {
      animation: backdrop-in 200ms ease-out;
    }
    .shell-drawer-backdrop-leaving {
      animation: backdrop-out 200ms ease-in forwards;
      pointer-events: none;
    }

    .assistant-dock {
      width: var(--assistant-dock-width);
    }

    .assistant-dock-leaving {
      pointer-events: none;
    }

    @media (prefers-reduced-motion: reduce) {
      .shell-grid {
        transition: none;
      }
      .shell-drawer,
      .shell-drawer-leaving,
      .shell-drawer-backdrop,
      .shell-drawer-backdrop-leaving {
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
      class="shell-grid bg-background fixed inset-x-0 top-0 grid h-dvh grid-rows-[60px_minmax(0,1fr)] overflow-hidden"
      [class.expanded]="!shell.isDrawer() && shell.sideNavExpanded()"
      [class.collapsed]="!shell.isDrawer() && shell.sideNavCollapsed()"
      [class.drawer]="shell.isDrawer()"
      [class.docked]="panel.isDocked()"
      [style.--assistant-dock-width]="dockWidth()"
      [style.transition]="panel.isResizing() ? 'none' : null">
      @if (!shell.isDrawer()) {
        <app-shell-sidebar
          class="col-start-1 row-span-2 row-start-1"
          (workspaceChange)="onWorkspaceChange($event)" />
      }
      <app-shell-navbar
        class="col-start-2 row-start-1"
        [attr.inert]="drawer.isPresent() ? '' : null" />

      <main
        class="relative isolate col-start-2 row-start-2 overflow-y-auto"
        [attr.inert]="drawer.isPresent() ? '' : null"
        [class.scrollbar-gutter-stable]="
          !boardBackground.imageUrl() && !pageOwnsScroll()
        ">
        @if (boardBackground.imageUrl(); as backgroundUrl) {
          <img
            [src]="backgroundUrl"
            alt=""
            aria-hidden="true"
            class="pointer-events-none absolute inset-0 -z-10 h-full w-full object-cover opacity-0 transition-opacity duration-500 ease-out motion-reduce:transition-none"
            [class.opacity-100]="boardBackgroundLoaded()"
            (load)="boardBackgroundLoaded.set(true)" />
          <div
            aria-hidden="true"
            class="bg-background/80 pointer-events-none absolute inset-0 -z-10"></div>
        }
        <router-outlet />
      </main>

      @if (dock.isPresent()) {
        <app-ai-assistant-panel
          class="assistant-dock border-border col-start-3 row-span-2 row-start-1 border-l"
          [class.assistant-dock-leaving]="dock.isLeaving()" />
      }
    </div>

    @if (drawer.isPresent()) {
      <div
        class="shell-drawer-backdrop fixed inset-0 z-40 bg-black/50"
        [class.shell-drawer-backdrop-leaving]="drawer.isLeaving()"
        aria-hidden="true"
        (click)="layout.closeSideMenu()"></div>
      <app-shell-sidebar
        class="shell-drawer bg-side-bar fixed inset-y-0 left-0 z-40 w-[min(85vw,300px)] shadow-xl"
        [class.shell-drawer-leaving]="drawer.isLeaving()"
        role="dialog"
        aria-modal="true"
        i18n-aria-label="Accessible name of the sidebar menu on small screens"
        aria-label="Menu"
        cdkTrapFocus
        [cdkTrapFocusAutoCapture]="true"
        (keydown.escape)="layout.closeSideMenu()"
        (workspaceChange)="onWorkspaceChange($event)" />
    }

    <app-command-palette></app-command-palette>
    <app-ai-assistant></app-ai-assistant>
  `,
})
export class ShellComponent {
  private router = inject(Router);

  protected readonly layout = inject(LayoutService);

  shell = inject(ShellService);
  readonly panel = inject(AiPanelService);
  readonly assistant = inject(AiAssistantService);
  readonly globalCommands = inject(GlobalCommandsService);
  readonly commandShortcuts = inject(CommandShortcutService);
  readonly preferences = inject(UserPreferencesService);
  readonly lastWorkspace = inject(LastWorkspaceService);
  readonly boardBackground = inject(BoardBackgroundService);

  readonly boardBackgroundLoaded = linkedSignal<string | null, boolean>({
    source: () => this.boardBackground.imageUrl(),
    computation: () => false,
  });

  authenticated = inject(SessionService).isAuthenticated;
  pageOwnsScroll = this.layout.pageOwnsScroll;

  readonly chunkLoading = signal(false);

  readonly dock = animatedPresence(this.panel.isDocked, DOCK_ANIMATION_MS);

  private readonly drawerOpen = computed(() => {
    return this.shell.isDrawer() && this.layout.sideMenuOpen();
  });

  readonly drawer = animatedPresence(this.drawerOpen, DRAWER_ANIMATION_MS);

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
