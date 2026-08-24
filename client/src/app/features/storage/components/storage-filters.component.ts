import { Component, computed, input, output } from '@angular/core';
import {
  WorkspaceFileContentTypeGroup,
  WorkspaceFileFilter,
  WorkspaceFilePurpose,
} from '@core/models/view-models/workspace-file-view-model';
import {
  LucideArrowUpDown,
  LucideFile,
  LucideListFilter,
  LucideX,
} from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { SearchInputComponent } from '@static/components/search-input/search-input.component';
import {
  SelectMenuComponent,
  SelectMenuOption,
} from '@static/components/select-menu/select-menu.component';

export type StorageSort = 'createdAt' | 'name' | 'sizeBytes';

const facetButtonClass =
  'border-border text-foreground hover:bg-foreground/5 h-9 min-w-0 gap-2 rounded-md border bg-transparent px-3 text-sm font-normal tracking-normal';

const activeFacetButtonClass =
  'border-primary/40 bg-primary/5 text-primary hover:bg-primary/10';

@Component({
  selector: 'app-storage-filters',
  imports: [
    FlatButtonComponent,
    LucideX,
    SearchInputComponent,
    SelectMenuComponent,
  ],
  template: `
    <div class="mb-4 flex flex-wrap items-center gap-2">
      <app-search-input
        [term]="filter().query"
        (searchChange)="queryChange.emit($event ?? '')" />

      <app-select-menu
        [options]="originOptions"
        [value]="filter().purpose"
        [icon]="originIcon"
        i18n-ariaLabel="Accessible label for the file origin filter"
        ariaLabel="Filter by origin"
        [buttonClass]="originButtonClass()"
        (valueChange)="purposeChange.emit($event)" />

      <app-select-menu
        [options]="contentTypeOptions"
        [value]="filter().contentTypeGroup"
        [icon]="contentTypeIcon"
        i18n-ariaLabel="Accessible label for the file type filter"
        ariaLabel="Filter by file type"
        [buttonClass]="contentTypeButtonClass()"
        (valueChange)="contentTypeChange.emit($event)" />

      <app-select-menu
        [options]="sortOptions"
        [value]="filter().sortBy"
        [icon]="sortIcon"
        i18n-ariaLabel="Accessible label for the file sort control"
        ariaLabel="Sort files"
        [buttonClass]="facetButtonClass"
        (valueChange)="emitSort($event)" />

      @if (hasActiveFilters()) {
        <button
          app-flat-button
          type="button"
          color="ghost"
          class="text-muted hover:text-foreground h-9 min-w-0 gap-1.5 rounded-md px-3 font-normal tracking-normal"
          (click)="resetFilters.emit()">
          <span i18n="Button that clears every active file filter">Reset</span>
          <svg lucideX class="h-4 w-4"></svg>
        </button>
      }
    </div>
  `,
})
export class StorageFiltersComponent {
  readonly filter = input.required<WorkspaceFileFilter>();

  readonly queryChange = output<string>();
  readonly purposeChange = output<WorkspaceFilePurpose | undefined>();
  readonly contentTypeChange = output<
    WorkspaceFileContentTypeGroup | undefined
  >();
  readonly sortChange = output<StorageSort>();
  readonly resetFilters = output();

  protected readonly facetButtonClass = facetButtonClass;
  protected readonly originIcon = LucideListFilter;
  protected readonly contentTypeIcon = LucideFile;
  protected readonly sortIcon = LucideArrowUpDown;

  protected readonly originOptions: readonly SelectMenuOption<
    WorkspaceFilePurpose | undefined
  >[] = [
    {
      label: $localize`:Label shown in the interface:All origins`,
      value: undefined,
    },
    {
      label: $localize`:Label shown in the interface:Task files`,
      value: WorkspaceFilePurpose.taskFile,
    },
    {
      label: $localize`:Label shown in the interface:Inline media`,
      value: WorkspaceFilePurpose.inlineMedia,
    },
    {
      label: $localize`:Label shown in the interface:Logos and backgrounds`,
      value: WorkspaceFilePurpose.branding,
    },
  ];

  protected readonly contentTypeOptions: readonly SelectMenuOption<
    WorkspaceFileContentTypeGroup | undefined
  >[] = [
    {
      label: $localize`:Label shown in the interface:All types`,
      value: undefined,
    },
    { label: $localize`:Label shown in the interface:Images`, value: 'image' },
    {
      label: $localize`:Label shown in the interface:Documents`,
      value: 'document',
    },
    {
      label: $localize`:Label shown in the interface:Archives`,
      value: 'archive',
    },
    { label: $localize`:Label shown in the interface:Other`, value: 'other' },
  ];

  protected readonly sortOptions: readonly SelectMenuOption<
    StorageSort | undefined
  >[] = [
    {
      label: $localize`:Label shown in the interface:Newest`,
      value: 'createdAt',
    },
    { label: $localize`:Label shown in the interface:Name`, value: 'name' },
    {
      label: $localize`:Label shown in the interface:Size`,
      value: 'sizeBytes',
    },
  ];

  protected readonly originButtonClass = computed(() => {
    return this.facetClass(this.filter().purpose !== undefined);
  });

  protected readonly contentTypeButtonClass = computed(() => {
    return this.facetClass(this.filter().contentTypeGroup !== undefined);
  });

  protected readonly hasActiveFilters = computed(() => {
    const filter = this.filter();

    return Boolean(
      filter.query || filter.purpose !== undefined || filter.contentTypeGroup
    );
  });

  protected emitSort(sortBy: StorageSort | undefined) {
    if (!sortBy) return;

    this.sortChange.emit(sortBy);
  }

  private facetClass(active: boolean): string {
    return active
      ? `${facetButtonClass} ${activeFacetButtonClass}`
      : facetButtonClass;
  }
}
