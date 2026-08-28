import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { TemplatePortal } from '@angular/cdk/portal';
import {
  Component,
  ElementRef,
  OnDestroy,
  TemplateRef,
  ViewContainerRef,
  computed,
  effect,
  inject,
  output,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { Router } from '@angular/router';
import { SessionService } from '@core/services/session.service';
import { DialogService } from '@core/services/dialog.service';
import { LastWorkspaceService } from '@core/services/last-workspace.service';
import { WorkspaceListService } from '@core/services/workspace-list.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { debounce, form } from '@angular/forms/signals';
import { ShellService } from '@app/shell/shell.service';
import { Workspace } from '@core/models/workspace';
import { brandingImageUrl } from '@core/util/branding';
import { filterObjectArray } from '@core/util/arrays';
import { AuthCommandsService } from '@core/services/auth-commands.service';
import { KeyboardService } from '@static/services/keyboard.service';
import { WorkspaceDialogComponent } from '@entry/dialogs/workspace-dialog/workspace-dialog.component';
import { WorkspaceBadgeComponent } from './workspace-badge.component';
import { animatedPresence } from '@core/util/animated-presence';
import { menuExitMs } from '@static/components/popover-surface/popover-surface.component';
import { WorkspaceSelectMenuComponent } from './workspace-select-menu.component';
import { LucideChevronsUpDown } from '@lucide/angular';

/** Width of the open menu, wider than the trigger it hangs off. */
const menuWidth = 264;

/** How many rows the RECENT group shows, the current workspace included. */
const maxRecentWorkspaces = 3;

@Component({
  selector: 'app-workspace-select',
  host: {
    class: 'block w-full h-15 shrink-0 py-2 px-2',
  },
  template: `
    <button
      class="hover:bg-side-bar-active/60 transition:background-color flex h-full w-full cursor-pointer items-center justify-center gap-4 overflow-hidden rounded px-2 text-sm font-medium text-white/70"
      [class.justify-start]="shell.sideNavExpanded()"
      [class.w-full]="shell.sideNavExpanded()"
      [class.text-left]="shell.sideNavExpanded()"
      [class.justify-center]="shell.sideNavCollapsed()"
      [class.mx-auto]="shell.sideNavCollapsed()"
      (click)="isAuthenticated() === true && toggleMenu()"
      #origin>
      @if (currentWorkspace(); as workspace) {
        <app-workspace-badge
          [color]="workspace.metaInfo?.color"
          [logoUrl]="currentLogoUrl()"
          [letter]="workspace.name[0]" />
        @if (shell.sideNavExpanded()) {
          <span class="min-w-0 flex-1 select-none">
            <span
              class="block truncate text-sm font-medium tracking-[.225px] text-white">
              {{ workspace.name }}
            </span>
            <span class="block truncate text-xs text-white/50">
              /{{ workspace.slug }}
            </span>
          </span>

          <svg lucideChevronsUpDown class="h-4 w-4 flex-none opacity-70"></svg>
        }
      }
    </button>

    <ng-template #menuTemplate>
      <app-workspace-select-menu
        [isOpen]="true"
        [leaving]="presence.isLeaving()"
        [filteredOptions]="filteredOptions()"
        [recentOptions]="recentWorkspaces()"
        [otherOptions]="otherWorkspaces()"
        [selected]="selected()"
        [current]="currentWorkspace()"
        [searchField]="searchForm.term"
        [searchTerm]="searchForm.term().value()"
        (optionSelect)="select($event)"
        (createWorkspace)="onCreateWorkspaceClicked()"
        (manage)="close()"
        (logout)="onlogOutClicked()" />
    </ng-template>
  `,
  imports: [
    WorkspaceBadgeComponent,
    WorkspaceSelectMenuComponent,
    LucideChevronsUpDown,
  ],
})
export class WorkspaceSelectComponent implements OnDestroy {
  private authCommands = inject(AuthCommandsService);
  private dialog = inject(DialogService);
  private keyboard = inject(KeyboardService);
  private lastWorkspace = inject(LastWorkspaceService);
  private overlay = inject(Overlay);
  private router = inject(Router);
  private vcr = inject(ViewContainerRef);

  shell = inject(ShellService);

  private readonly originRef =
    viewChild.required<ElementRef<HTMLElement>>('origin');
  private readonly menuTemplate =
    viewChild.required<TemplateRef<unknown>>('menuTemplate');
  private overlayRef?: OverlayRef;

  readonly selectChange = output<Workspace>();
  readonly closed = output();

  readonly workspaces = inject(WorkspaceListService).workspaces;
  readonly currentWorkspace = inject(CurrentWorkspaceService).workspace;
  readonly workspaceId = inject(CurrentWorkspaceService).id;

  readonly isAuthenticated = inject(SessionService).isAuthenticated;

  readonly currentLogoUrl = computed(() => {
    const workspace = this.currentWorkspace();

    return brandingImageUrl(workspace?.slug, workspace?.metaInfo?.logoFileId);
  });

  /**
   * The current workspace, then the most recently visited ones behind it. The
   * stored history already leads with the current workspace once it has been
   * written, but it lags a fresh switch and is empty on a first visit.
   */
  readonly recentWorkspaces = computed(() => {
    const workspaces = this.workspaces();
    const current = this.currentWorkspace();

    const remembered = this.lastWorkspace
      .recentIds()
      .map((id) => workspaces.find((workspace) => workspace.id === id))
      .filter((workspace): workspace is Workspace => !!workspace);

    const ordered = current
      ? [
          current,
          ...remembered.filter((workspace) => workspace.id !== current.id),
        ]
      : remembered;

    return ordered.slice(0, maxRecentWorkspaces);
  });

  readonly otherWorkspaces = computed(() => {
    const recentIds = new Set(
      this.recentWorkspaces().map((workspace) => workspace.id)
    );

    return this.workspaces().filter(
      (workspace) => !recentIds.has(workspace.id)
    );
  });

  /** Arrow keys walk the rows in the order they are rendered. */
  readonly navigationOptions = computed(() => {
    if (this.searchForm.term().value()) {
      return this.filteredOptions();
    }

    return [...this.recentWorkspaces(), ...this.otherWorkspaces()];
  });

  filteredOptions = computed(() => {
    const options = this.workspaces();
    const term = this.searchForm.term().value();
    if (!term) {
      return options;
    }
    return filterObjectArray(options, 'name', term);
  });

  searchFormModel = signal({
    term: '',
  });

  searchForm = form(this.searchFormModel, (schema) => {
    debounce(schema.term, 300);
  });

  isOpen = signal(false);
  selected = signal<Workspace | null>(null);

  /** Keeps the overlay attached while the exit animation plays. */
  protected readonly presence = animatedPresence(this.isOpen, menuExitMs);

  constructor() {
    effect(() => {
      const event = this.keyboard.keyDown();

      if (!event) {
        return;
      }

      untracked(() => {
        if (this.isOpen()) {
          this.handleKeyDown(event);
        }
      });
    });

    effect(() => {
      if (this.searchForm.term().value()) {
        this.selectNextOptiom();
      }
    });

    effect(() => {
      if (this.presence.isPresent()) return;

      // Resetting the query here rather than in close() keeps the list from
      // repopulating behind the fade.
      untracked(() => {
        this.overlayRef?.detach();
        this.searchForm.term().value.set('');
      });
    });
  }

  handleKeyDown(event: KeyboardEvent) {
    // The search field is focused while the menu is open, so the letter
    // shortcuts only fire when there is nothing to type them into. Modifiers
    // are left to the browser — ctrl+n and ctrl+shift+w are its own.
    const blocked =
      !!this.searchForm.term().value() ||
      event.ctrlKey ||
      event.metaKey ||
      event.altKey;

    switch (event.key) {
      case 'ArrowUp':
        this.selectPreviousOption();
        break;
      case 'ArrowDown':
        this.selectNextOptiom();
        break;
      case 'Enter':
        this.select();
        break;
      case 'Escape':
        this.close();
        break;
      case 'n':
      case 'N':
        if (blocked) break;
        this.onCreateWorkspaceClicked();
        break;
      case 'W':
        if (blocked || !event.shiftKey) break;
        this.onManageWorkspacesClicked();
        break;
    }
  }

  selectNextOptiom() {
    const options = this.navigationOptions();

    if (!this.selected()) {
      this.selected.set(options[0]);
    } else {
      const currentIndex = options.findIndex(
        (opt) => opt.id === this.selected()?.id
      );

      if (options.length === currentIndex + 1) {
        return;
      }

      this.selected.set(options[currentIndex + 1]);
    }
  }

  selectPreviousOption() {
    const options = this.navigationOptions();
    const selected = this.selected();

    if (!options) return;

    if (!selected) {
      this.selected.set((options?.length && options[0]) || null);
    } else {
      const index = options?.findIndex((opt) => opt.id === selected.id) ?? -1;

      if (index === 0 || index === -1) {
        return;
      }

      this.selected.set(options[index - 1]);
    }
  }

  toggleMenu() {
    if (this.isOpen()) {
      this.close();

      return;
    }

    this.openMenu();
  }

  private openMenu() {
    // A menu still playing its exit gets a fresh overlay rather than being
    // revived: its backdrop has already been dropped and cannot be re-armed.
    this.overlayRef?.dispose();

    const originEl = this.originRef().nativeElement;
    const collapsed = this.shell.sideNavCollapsed();

    const positionStrategy = this.overlay
      .position()
      .flexibleConnectedTo(originEl)
      .withPositions(
        collapsed
          ? [
              {
                originX: 'end',
                originY: 'top',
                overlayX: 'start',
                overlayY: 'top',
                offsetX: 8,
                offsetY: 8,
              },
            ]
          : [
              {
                originX: 'start',
                originY: 'bottom',
                overlayX: 'start',
                overlayY: 'top',
                offsetX: 8,
                offsetY: 8,
              },
            ]
      );

    this.overlayRef = this.overlay.create({
      positionStrategy,
      hasBackdrop: true,
      backdropClass: 'cdk-overlay-transparent-backdrop',
      scrollStrategy: this.overlay.scrollStrategies.reposition(),
      width: menuWidth,
    });

    this.overlayRef.attach(new TemplatePortal(this.menuTemplate(), this.vcr));
    this.overlayRef.backdropClick().subscribe(() => this.close());
    this.isOpen.set(true);
  }

  close() {
    if (!this.isOpen()) return;

    this.closed.emit();
    this.isOpen.set(false);

    // The backdrop would otherwise swallow clicks for the length of the fade.
    this.overlayRef?.detachBackdrop();
  }

  select(option: Workspace | null = null) {
    this.selected.set(option ?? this.selected());

    const selected = this.selected();

    if (this.isOpen() && selected) {
      this.selectChange.emit(selected);
      this.close();
    }

    this.selected.set(null);
  }

  isActive(option: Workspace) {
    if (!this.selected()) {
      return false;
    }
    return option.id === this.selected()?.id;
  }

  onCreateWorkspaceClicked() {
    this.close();

    this.dialog.openWizard(WorkspaceDialogComponent, {
      title: $localize`:Title of a dialog or section:Create Workspace`,
      data: null,
      width: '720px',
    });
  }

  onManageWorkspacesClicked() {
    this.close();
    void this.router.navigate(['/workspaces']);
  }

  onlogOutClicked() {
    this.close();
    this.authCommands.logout();
  }

  ngOnDestroy() {
    this.overlayRef?.dispose();
  }
}
