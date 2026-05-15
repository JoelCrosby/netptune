import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import {
  LucideFolderOpen,
  LucideHash,
  LucideKanban,
  LucideLayers,
  LucideSearch,
} from '@lucide/angular';
import { SearchResult } from '@core/models/search-result';

@Component({
  selector: 'app-search-result-item',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideFolderOpen, LucideHash, LucideKanban, LucideLayers, LucideSearch],
  template: `
    <button
      type="button"
      class="relative flex w-full cursor-default select-none items-center gap-2 rounded-sm px-2 py-1.5 text-sm outline-none aria-selected:bg-accent aria-selected:text-accent-foreground"
      [attr.aria-selected]="selected() || null"
      (click)="activate.emit(result())"
      (mouseenter)="hover.emit()">
      @switch (result().type) {
        @case ('task') { <svg lucideHash class="h-4 w-4 shrink-0 opacity-50"></svg> }
        @case ('project') { <svg lucideFolderOpen class="h-4 w-4 shrink-0 opacity-50"></svg> }
        @case ('board') { <svg lucideKanban class="h-4 w-4 shrink-0 opacity-50"></svg> }
        @case ('sprint') { <svg lucideLayers class="h-4 w-4 shrink-0 opacity-50"></svg> }
        @default { <svg lucideSearch class="h-4 w-4 shrink-0 opacity-50"></svg> }
      }
      <span class="flex-1 overflow-hidden text-left">
        <span class="block truncate">{{ result().title }}</span>
        <span class="block truncate text-xs text-muted-foreground">{{ result().subtitle }}</span>
      </span>
    </button>
  `,
})
export class SearchResultItemComponent {
  result = input.required<SearchResult>();
  selected = input(false);
  activate = output<SearchResult>();
  hover = output();
}
