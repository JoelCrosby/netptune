import { Component, ElementRef, inject, input, output } from '@angular/core';
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
      label="Filter by Tag"
      [icon]="lucideTag"
      [color]="selectedCount() ? 'primary' : undefined"
      [count]="selectedCount()"
      (action)="opened.emit(); menu.toggle(el.nativeElement)" />

    <app-dropdown-menu #menu>
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
            <span class="font-medium">No tags</span>
            <p class="text-center text-xs">
              Tags assigned to tasks will show here
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

  readonly opened = output();
  readonly toggled = output<Selected<Tag>>();
}
