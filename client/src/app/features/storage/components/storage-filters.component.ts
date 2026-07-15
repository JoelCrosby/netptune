import { Component, input, output } from '@angular/core';
import {
  WorkspaceFileFilter,
  WorkspaceFilePurpose,
} from '@core/models/view-models/workspace-file-view-model';
import { LucideCheck } from '@lucide/angular';
import { DropdownButtonComponent } from '@static/components/dropdown-menu/dropdown-button.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { SearchInputComponent } from '@static/components/search-input/search-input.component';

export type StorageSort = 'createdAt' | 'name' | 'sizeBytes';

@Component({
  selector: 'app-storage-filters',
  imports: [
    DropdownButtonComponent,
    LucideCheck,
    MenuItemComponent,
    SearchInputComponent,
  ],
  template: `
    <div class="mb-4 flex flex-wrap items-start gap-3">
      <app-search-input
        [term]="filter().query"
        (searchChange)="queryChange.emit($event ?? '')" />

      <app-dropdown-button
        #originMenu
        [label]="originLabel()"
        ariaLabel="Filter by origin"
        buttonClass="w-44 justify-between">
        @for (option of originOptions; track option.label) {
          <button
            app-menu-item
            type="button"
            role="menuitemradio"
            [attr.aria-checked]="filter().purpose === option.value"
            (click)="purposeChange.emit(option.value); originMenu.close()">
            <span class="flex h-4 w-4 items-center justify-center">
              @if (filter().purpose === option.value) {
                <svg lucideCheck class="h-4 w-4"></svg>
              }
            </span>
            <span>{{ option.label }}</span>
          </button>
        }
      </app-dropdown-button>

      <app-dropdown-button
        #sortMenu
        [label]="sortLabel()"
        ariaLabel="Sort files"
        buttonClass="w-44 justify-between">
        @for (option of sortOptions; track option.value) {
          <button
            app-menu-item
            type="button"
            role="menuitemradio"
            [attr.aria-checked]="filter().sortBy === option.value"
            (click)="sortChange.emit(option.value); sortMenu.close()">
            <span class="flex h-4 w-4 items-center justify-center">
              @if (filter().sortBy === option.value) {
                <svg lucideCheck class="h-4 w-4"></svg>
              }
            </span>
            <span>{{ option.label }}</span>
          </button>
        }
      </app-dropdown-button>
    </div>
  `,
})
export class StorageFiltersComponent {
  readonly filter = input.required<WorkspaceFileFilter>();

  readonly queryChange = output<string>();
  readonly purposeChange = output<WorkspaceFilePurpose | undefined>();
  readonly sortChange = output<StorageSort>();

  protected readonly originOptions: readonly {
    label: string;
    value: WorkspaceFilePurpose | undefined;
  }[] = [
    { label: 'All origins', value: undefined },
    { label: 'Task files', value: WorkspaceFilePurpose.taskFile },
    { label: 'Inline media', value: WorkspaceFilePurpose.inlineMedia },
  ];

  protected readonly sortOptions: readonly {
    label: string;
    value: StorageSort;
  }[] = [
    { label: 'Newest', value: 'createdAt' },
    { label: 'Name', value: 'name' },
    { label: 'Size', value: 'sizeBytes' },
  ];

  protected originLabel(): string {
    return (
      this.originOptions.find(
        (option) => option.value === this.filter().purpose
      )?.label ?? 'All origins'
    );
  }

  protected sortLabel(): string {
    return (
      this.sortOptions.find((option) => option.value === this.filter().sortBy)
        ?.label ?? 'Newest'
    );
  }
}
