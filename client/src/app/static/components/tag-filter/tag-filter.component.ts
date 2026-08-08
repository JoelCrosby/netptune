import {
  Component,
  computed,
  ElementRef,
  inject,
  input,
  output,
} from '@angular/core';
import { Selected } from '@core/models/selected';
import { Tag } from '@core/models/tag';
import { LucideTag } from '@lucide/angular';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuCheckboxItemComponent } from '@static/components/dropdown-menu/menu-checkbox-item.component';
import { FilterActionButtonComponent } from '@static/components/filter-action-button/filter-action-button.component';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';

@Component({
  selector: 'app-tag-filter',
  imports: [
    FilterActionButtonComponent,
    DropdownMenuComponent,
    MenuCheckboxItemComponent,
    SpinnerComponent,
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

    <app-dropdown-menu #menu>
      <button
        app-menu-checkbox-item
        [checked]="untagged()"
        (checkedChange)="untaggedChange.emit($event)">
        <span i18n="Filters the list down to tasks that carry no tags">
          Untagged
        </span>
      </button>

      <div
        class="my-1 border-t border-neutral-200 dark:border-neutral-700"></div>

      @if (loaded()) {
        @if (tags().length) {
          @for (tag of tags(); track tag.id) {
            <button
              app-menu-checkbox-item
              [checked]="tag.selected ?? false"
              (checkedChange)="toggled.emit(tag)">
              {{ tag.name }}
            </button>
          }
        } @else {
          <div
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
        }
      } @else {
        <div class="flex justify-center p-4">
          <app-spinner diameter="1.5rem" />
        </div>
      }
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
}
