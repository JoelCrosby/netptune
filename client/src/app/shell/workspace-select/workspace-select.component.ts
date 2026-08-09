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
import { SessionService } from '@core/services/session.service';
import { WorkspaceListService } from '@core/services/workspace-list.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { debounce, form } from '@angular/forms/signals';
import { ShellService } from '@app/shell/shell.service';
import { Workspace } from '@core/models/workspace';
import { filterObjectArray } from '@core/util/arrays';
import { AuthCommandsService } from '@core/services/auth-commands.service';
import { KeyboardService } from '@static/services/keyboard.service';
import { WorkspaceBadgeComponent } from './workspace-badge.component';
import { WorkspaceSelectMenuComponent } from './workspace-select-menu.component';
import { LucideChevronsUpDown } from '@lucide/angular';

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
        [filteredOptions]="filteredOptions()"
        [workspaces]="workspaces()"
        [selected]="selected()"
        [current]="currentWorkspace()"
        [searchField]="searchForm.term"
        (optionSelect)="select($event)"
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
  private keyboard = inject(KeyboardService);
  private overlay = inject(Overlay);
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
  }

  handleKeyDown(event: KeyboardEvent) {
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
    }
  }

  selectNextOptiom() {
    const options = this.filteredOptions();

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
    const options = this.filteredOptions();
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
    if (this.overlayRef?.hasAttached()) {
      this.close();
    } else {
      this.openMenu();
    }
  }

  private openMenu() {
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
      width: collapsed ? 200 : originEl.offsetWidth,
    });

    this.overlayRef.attach(new TemplatePortal(this.menuTemplate(), this.vcr));
    this.overlayRef.backdropClick().subscribe(() => this.close());
    this.isOpen.set(true);
  }

  close() {
    this.closed.emit();
    this.overlayRef?.detach();
    this.isOpen.set(false);
    this.searchForm.term().value.set('');
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

  onlogOutClicked() {
    this.close();
    this.authCommands.logout();
  }

  ngOnDestroy() {
    this.overlayRef?.dispose();
  }
}
