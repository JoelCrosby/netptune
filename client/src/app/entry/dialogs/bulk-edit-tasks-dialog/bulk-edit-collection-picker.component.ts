import {
  Component,
  afterNextRender,
  computed,
  input,
  output,
  signal,
} from '@angular/core';
import { BulkCollectionMode } from '@core/enums/bulk-collection-mode';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import {
  FilterOption,
  FilterOptionListComponent,
} from '@static/components/filter-option-list/filter-option-list.component';
import {
  SelectMenuComponent,
  SelectMenuOption,
} from '@static/components/select-menu/select-menu.component';

export interface BulkEditPickerOption extends FilterOption<string> {
  pictureUrl?: string | null;
  isServiceAccount?: boolean;
}

const modeButtonClass =
  'h-6.5 min-w-0 shrink-0 gap-1.5 rounded-none border-r border-foreground/14 px-0 pr-2 text-[13px] font-medium tracking-normal text-foreground/60 hover:bg-transparent hover:text-foreground';

@Component({
  selector: 'app-bulk-edit-collection-picker',
  imports: [AvatarComponent, FilterOptionListComponent, SelectMenuComponent],
  host: { class: 'block' },
  template: `
    <ng-template #avatarSlot let-option>
      <app-avatar
        class="shrink-0"
        size="sm"
        [tooltip]="false"
        [name]="option.label"
        [imageUrl]="option.pictureUrl"
        [isServiceAccount]="option.isServiceAccount ?? false" />
    </ng-template>

    <div
      class="border-border bg-form-field-background overflow-hidden rounded-sm border-2">
      <app-filter-option-list
        class="w-full!"
        [listMaxHeightClass]="listMaxHeightClass"
        [open]="opened()"
        [dismissKeyHint]="false"
        [options]="options()"
        [selected]="selectedValues()"
        [optionLeading]="avatars() ? avatarSlot : undefined"
        [searchPlaceholder]="searchPlaceholder()"
        [listAriaLabel]="listAriaLabel()"
        [emptyMessage]="emptyMessage()"
        (toggled)="toggled.emit($event)">
        <app-select-menu
          searchPrefix
          color="ghost"
          xPosition="after"
          [buttonClass]="modeButtonClass"
          [options]="modeOptions"
          [value]="mode()"
          [ariaLabel]="modeAriaLabel()"
          (valueChange)="modeChange.emit($event)" />

        <span searchSuffix class="text-muted shrink-0 text-xs font-medium">
          {{ selectedCountLabel() }}
        </span>

        <button
          searchSuffix
          type="button"
          class="text-primary shrink-0 cursor-pointer px-0.5 text-xs font-medium hover:underline disabled:cursor-default disabled:opacity-40 disabled:hover:no-underline"
          [disabled]="!selected().length"
          (click)="cleared.emit()">
          <span
            i18n="
              Button that unpicks everything in a bulk edit's tag or people
              picker
            ">
            Clear
          </span>
        </button>
      </app-filter-option-list>
    </div>
  `,
})
export class BulkEditCollectionPickerComponent {
  readonly options = input.required<readonly BulkEditPickerOption[]>();
  readonly selected = input.required<readonly string[]>();
  readonly mode = input.required<BulkCollectionMode>();
  readonly avatars = input(false);
  readonly searchPlaceholder = input.required<string>();
  readonly listAriaLabel = input.required<string>();
  readonly modeAriaLabel = input.required<string>();
  readonly emptyMessage = input.required<string>();

  readonly toggled = output<string>();
  readonly cleared = output();
  readonly modeChange = output<BulkCollectionMode>();

  // The list takes focus when it opens, which it cannot do until its own view exists. A dropdown
  // host only ever opens one after render; an inline one has to wait for the same moment.
  protected readonly opened = signal(false);

  protected readonly modeButtonClass = modeButtonClass;
  protected readonly listMaxHeightClass = 'max-h-34';

  protected readonly modeOptions: SelectMenuOption<BulkCollectionMode>[] = [
    {
      value: BulkCollectionMode.add,
      label: $localize`:Bulk edit mode that leaves a task's existing tags or assignees in place:Add to`,
    },
    {
      value: BulkCollectionMode.replace,
      label: $localize`:Bulk edit mode that swaps out a task's existing tags or assignees:Replace with`,
    },
  ];

  protected readonly selectedValues = computed(() => new Set(this.selected()));

  protected readonly selectedCountLabel = computed(() => {
    const count = this.selected().length;

    return $localize`:How many entries are picked in a bulk edit's tag or people picker. COUNT is the number picked:${count}:COUNT: selected`;
  });

  constructor() {
    afterNextRender(() => this.opened.set(true));
  }
}
