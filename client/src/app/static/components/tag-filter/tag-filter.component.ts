import {
  Component,
  ElementRef,
  computed,
  inject,
  input,
  output,
} from '@angular/core';
import { Selected } from '@core/models/selected';
import { Tag } from '@core/models/tag';
import { LucideTag } from '@lucide/angular';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { FilterActionButtonComponent } from '@static/components/filter-action-button/filter-action-button.component';
import {
  FilterOption,
  FilterOptionListComponent,
} from '@static/components/filter-option-list/filter-option-list.component';

const untaggedValue = 'untagged';

// Every tag is namespaced so no tag name can collide with the untagged entry.
function tagValue(name: string): string {
  return `tag:${name}`;
}

@Component({
  selector: 'app-tag-filter',
  imports: [
    FilterActionButtonComponent,
    DropdownMenuComponent,
    FilterOptionListComponent,
    LucideTag,
  ],
  template: `
    <app-filter-action-button
      i18n-label="Label on the control that filters tasks by tag"
      label="Filter by Tag"
      [icon]="lucideTag"
      [color]="activeCount() ? 'primary' : undefined"
      [count]="activeCount()"
      (action)="menu.toggle(el.nativeElement)" />

    <app-dropdown-menu #menu panelRole="none" [panelClass]="'p-0'">
      <app-filter-option-list
        [open]="menu.showing()"
        [options]="options()"
        [selected]="selectedValues()"
        [loading]="!loaded()"
        i18n-searchPlaceholder="Placeholder in the box that searches tags"
        searchPlaceholder="Search tags"
        i18n-listAriaLabel="Accessible name of the tag filter's option list"
        listAriaLabel="Tags"
        (toggled)="onToggled($event)"
        (dismissed)="menu.closeAndFocusTrigger()">
        <div
          emptyState
          class="flex flex-col items-center gap-1 px-4 py-3 text-sm opacity-60 select-none">
          <svg lucideTag class="mb-1 h-5 w-5 opacity-60"></svg>
          <span
            class="font-medium"
            i18n="Heading of the empty state in the tag filter">
            No tags
          </span>
          <p class="text-center text-xs">
            <span i18n="Explains why the tag filter is empty">
              Tags assigned to tasks will show here
            </span>
          </p>
        </div>
      </app-filter-option-list>
    </app-dropdown-menu>
  `,
})
export class TagFilterComponent {
  readonly el = inject(ElementRef);

  readonly lucideTag = LucideTag;

  readonly tags = input<Selected<Tag>[]>([]);
  readonly loaded = input(false);
  readonly selectedCount = input(0);
  readonly untagged = input(false);

  readonly toggled = output<Selected<Tag>>();
  readonly untaggedChange = output<boolean>();

  readonly activeCount = computed(() => {
    return this.selectedCount() + (this.untagged() ? 1 : 0);
  });

  protected readonly options = computed<FilterOption<string>[]>(() => {
    const untagged: FilterOption<string> = {
      value: untaggedValue,
      label: $localize`:Filters the list down to tasks that carry no tags:Untagged`,
      sticky: true,
    };

    const tags = this.tags().map((tag) => ({
      value: tagValue(tag.name),
      label: tag.name,
    }));

    return [untagged, ...tags];
  });

  protected readonly selectedValues = computed(() => {
    const selected = new Set(
      this.tags()
        .filter((tag) => tag.selected)
        .map((tag) => tagValue(tag.name))
    );

    if (this.untagged()) {
      selected.add(untaggedValue);
    }

    return selected;
  });

  protected onToggled(value: string) {
    if (value === untaggedValue) {
      this.untaggedChange.emit(!this.untagged());

      return;
    }

    const tag = this.tags().find(
      (candidate) => tagValue(candidate.name) === value
    );

    if (!tag) return;

    this.toggled.emit(tag);
  }
}
