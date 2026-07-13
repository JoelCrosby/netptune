import { Component, input, output } from '@angular/core';
import {
  LucideFolderOpen,
  LucideHash,
  LucideKanban,
  LucideLayers,
  LucideSearch,
} from '@lucide/angular';
import { SearchResult } from '@core/models/search-result';
import { CommandPaletteItemComponent } from './command-palette-item.component';

@Component({
  selector: 'app-search-result-item',
  imports: [
    LucideFolderOpen,
    LucideHash,
    LucideKanban,
    LucideLayers,
    LucideSearch,
    CommandPaletteItemComponent,
  ],
  template: `
    <button
      app-command-palette-item
      [selected]="selected()"
      (click)="activate.emit(result())"
      (mouseenter)="hover.emit()">
      @switch (result().type) {
        @case ('task') {
          <svg lucideHash class="h-4 w-4 shrink-0 opacity-50"></svg>
        }
        @case ('project') {
          <svg lucideFolderOpen class="h-4 w-4 shrink-0 opacity-50"></svg>
        }
        @case ('board') {
          <svg lucideKanban class="h-4 w-4 shrink-0 opacity-50"></svg>
        }
        @case ('sprint') {
          <svg lucideLayers class="h-4 w-4 shrink-0 opacity-50"></svg>
        }
        @default {
          <svg lucideSearch class="h-4 w-4 shrink-0 opacity-50"></svg>
        }
      }
      <span class="flex-1 overflow-hidden text-left text-sm">
        <span class="block truncate">{{ result().title }}</span>
        <span class="text-muted-foreground block truncate">{{
          result().subtitle
        }}</span>
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
