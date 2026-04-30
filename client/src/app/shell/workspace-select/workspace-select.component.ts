import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { TemplatePortal } from '@angular/cdk/portal';
import {
  ChangeDetectionStrategy,
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
import { debounce, form } from '@angular/forms/signals';
import { selectIsAuthenticated } from '@app/core/store/auth/auth.selectors';
import { ShellService } from '@app/shell/shell.service';
import { logout } from '@app/core/store/auth/auth.actions';
import { Workspace } from '@core/models/workspace';
import {
  selectAllWorkspaces,
  selectCurrentWorkspace,
  selectCurrentWorkspaceId,
} from '@core/store/workspaces/workspaces.selectors';
import { filterObjectArray } from '@core/util/arrays';
import { Store } from '@ngrx/store';
import { KeyboardService } from '@static/services/keyboard.service';
import { WorkspaceBadgeComponent } from './workspace-badge.component';
import { WorkspaceSelectMenuComponent } from './workspace-select-menu.component';

@Component({
  selector: 'app-workspace-select',
  host: { class: 'block w-full border-side-bar-border border-b h-15' },
  template: `
    <button
      class="hover:bg-side-bar-active/60 transition:background-color flex w-full cursor-pointer items-center justify-center gap-4 overflow-hidden py-4 text-sm font-medium text-white/70"
      [class.px-6]="shell.sideNavExpanded()"
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
          <div
            class="w-full overflow-hidden text-base font-medium tracking-[.225px] text-ellipsis whitespace-nowrap text-white select-none">
            {{ workspace.name }}
          </div>
        }
      }
    </button>

    <ng-template #menuTemplate>
      <app-workspace-select-menu
        [isOpen]="true"
        [filteredOptions]="filteredOptions()"
        [workspaces]="workspaces()"
        [selected]="selected()"
        [searchField]="searchForm.term"
        (optionSelect)="select($event)"
        (logout)="onlogOutClicked()" />
    </ng-template>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [WorkspaceBadgeComponent, WorkspaceSelectMenuComponent],
})
export class WorkspaceSelectComponent implements OnDestroy {
  private store = inject(Store);
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

  readonly workspaces = this.store.selectSignal(selectAllWorkspaces);
  readonly currentWorkspace = this.store.selectSignal(selectCurrentWorkspace);
  readonly workspaceId = this.store.selectSignal(selectCurrentWorkspaceId);

  readonly isAuthenticated = this.store.selectSignal(selectIsAuthenticated);

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
    this.store.dispatch(logout());
  }

  ngOnDestroy() {
    this.overlayRef?.dispose();
  }
}
