import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  ElementRef,
  inject,
  output,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { debounce, form } from '@angular/forms/signals';
import { selectIsAuthenticated } from '@app/core/auth/store/auth.selectors';
import { ShellService } from '@app/shell/shell.service';
import { logout } from '@core/auth/store/auth.actions';
import { Workspace } from '@core/models/workspace';
import {
  selectAllWorkspaces,
  selectCurrentWorkspace,
  selectCurrentWorkspaceId,
} from '@core/store/workspaces/workspaces.selectors';
import { filterObjectArray } from '@core/util/arrays';
import { Store } from '@ngrx/store';
import { DocumentService } from '@static/services/document.service';
import { KeyboardService } from '@static/services/keyboard.service';
import { WorkspaceBadgeComponent } from './workspace-badge.component';
import { WorkspaceSelectMenuComponent } from './workspace-select-menu.component';

@Component({
  selector: 'app-workspace-select',
  host: { class: 'block w-full border-side-bar-border border-b h-15' },
  template: `
    <div class="relative" #dropdown>
      <button
        class="hover:bg-side-bar-active/60 transition:background-color flex w-full cursor-pointer items-center justify-center gap-4 overflow-hidden py-4 text-sm font-medium text-white/70"
        [class.px-6]="shell.sideNavExpanded()"
        [class.justify-start]="shell.sideNavExpanded()"
        [class.w-full]="shell.sideNavExpanded()"
        [class.text-left]="shell.sideNavExpanded()"
        [class.justify-center]="shell.sideNavCollapsed()"
        [class.mx-auto]="shell.sideNavCollapsed()"
        (click)="isAuthenticated() === true && open(selectmenu, origin)"
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

      <div
        #selectmenu
        class="absolute top-13.75 left-0 overflow-hidden rounded-sm shadow-[0_2px_4px_-1px_rgb(0_0_0/20%),0_4px_5px_0_rgb(0_0_0/14%),0_1px_10px_0_rgb(0_0_0/12%)]">
        <app-workspace-select-menu
          [isOpen]="isOpen()"
          [filteredOptions]="filteredOptions()"
          [workspaces]="workspaces()"
          [selected]="selected()"
          [searchField]="searchForm.term"
          (optionSelect)="select($event)"
          (logout)="onlogOutClicked()" />
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [WorkspaceBadgeComponent, WorkspaceSelectMenuComponent],
})
export class WorkspaceSelectComponent {
  private store = inject(Store);
  private document = inject(DocumentService);
  private keyboard = inject(KeyboardService);

  shell = inject(ShellService);

  readonly dropdownElementRef = viewChild.required<ElementRef>('dropdown');

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
      const el = this.document.documentClicked();
      untracked(() => this.handleDocumentClick(el));
    });

    effect(() => {
      const event = this.keyboard.keyDown();
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

  handleDocumentClick(target: EventTarget) {
    if (!this.dropdownElementRef().nativeElement.contains(target)) {
      this.close();
    }
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
      const firstOption = options[0];
      this.selected.set(firstOption);
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

  open(dropdown: HTMLElement, origin: HTMLElement) {
    this.isOpen.set(true);

    if (this.shell.sideNavCollapsed()) {
      dropdown.style.width = '200px';
      dropdown.style.left = '80px';
      dropdown.style.top = '0px';
    } else {
      dropdown.style.width = `${origin.offsetWidth}px`;
      dropdown.style.left = '0';
      dropdown.style.top = '55px';
    }
  }

  close() {
    this.closed.emit();
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
}
