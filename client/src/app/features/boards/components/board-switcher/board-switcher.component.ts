import {
  Component,
  ElementRef,
  computed,
  inject,
  viewChild,
} from '@angular/core';
import { Router } from '@angular/router';
import { workspaceBoardsResource } from '@core/resources/board.resource';
import { BoardViewService } from '@core/services/board-view.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { LucideChevronDown } from '@lucide/angular';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import {
  FilterOption,
  FilterOptionListComponent,
} from '@static/components/filter-option-list/filter-option-list.component';

@Component({
  selector: 'app-board-switcher',
  host: { class: 'contents' },
  imports: [
    DropdownMenuComponent,
    FilterOptionListComponent,
    IconButtonComponent,
    LucideChevronDown,
  ],
  template: `
    @if (canSwitch()) {
      <button
        #trigger
        type="button"
        app-icon-button
        class="text-foreground/70 hover:text-foreground ml-1 h-7 w-7"
        aria-haspopup="menu"
        [attr.aria-expanded]="menu.showing()"
        [attr.aria-label]="triggerLabel"
        [title]="triggerLabel"
        (click)="toggleMenu(menu)">
        <svg lucideChevronDown class="h-5 w-5"></svg>
      </button>

      <app-dropdown-menu #menu panelRole="none" panelClass="p-0">
        <app-filter-option-list
          [open]="menu.showing()"
          [multiple]="false"
          widthClass="w-[min(24rem,calc(100vw-2rem))]"
          [options]="options()"
          [selected]="selected()"
          [listAriaLabel]="triggerLabel"
          [searchPlaceholder]="searchPlaceholder"
          (toggled)="onBoardSelected($event, menu)"
          (dismissed)="menu.closeAndFocusTrigger()" />
      </app-dropdown-menu>
    }
  `,
})
export class BoardSwitcherComponent {
  private readonly router = inject(Router);
  private readonly workspaceId = inject(CurrentWorkspaceService).slug;
  private readonly openIdentifier = inject(BoardViewService).identifier;

  private readonly boards = workspaceBoardsResource();

  // Read as an ElementRef: #trigger resolves to the button component, and the
  // menu positions against the DOM node.
  private readonly trigger = viewChild('trigger', { read: ElementRef });

  protected readonly triggerLabel = $localize`:Tooltip and accessible label for the button that switches to another board:Switch board`;
  protected readonly searchPlaceholder = $localize`:Placeholder in the box that narrows the list of boards:Search boards`;

  protected readonly options = computed<FilterOption<string>[]>(() => {
    return this.boards.value().flatMap((group) => {
      return group.boards.map((board) => ({
        value: board.identifier,
        label: board.name,
        hint: group.projectName,
      }));
    });
  });

  protected readonly selected = computed(() => {
    const identifier = this.openIdentifier();

    return new Set(identifier ? [identifier] : []);
  });

  protected readonly canSwitch = computed(() => {
    return this.boards.canRead() && this.options().length > 1;
  });

  protected toggleMenu(menu: DropdownMenuComponent) {
    const trigger = this.trigger();

    if (!trigger) return;

    menu.toggle(trigger.nativeElement);
  }

  protected onBoardSelected(identifier: string, menu: DropdownMenuComponent) {
    menu.close();

    if (identifier === this.openIdentifier()) return;

    void this.router.navigate(['/', this.workspaceId(), 'boards', identifier]);
  }
}
