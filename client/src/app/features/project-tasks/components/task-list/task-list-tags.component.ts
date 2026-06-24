import { Component, ElementRef, inject } from '@angular/core';
import { Selected } from '@core/models/selected';
import { Tag } from '@core/models/tag';
import * as TagActions from '@core/store/tags/tags.actions';
import * as TagSelectors from '@core/store/tags/tags.selectors';
import { LucideTag } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuCheckboxItemComponent } from '@static/components/dropdown-menu/menu-checkbox-item.component';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import { TaskListFilterActionComponent } from './task-list-filter-action.component';

@Component({
  selector: 'app-task-list-tags',
  imports: [
    TaskListFilterActionComponent,
    DropdownMenuComponent,
    MenuCheckboxItemComponent,
    SpinnerComponent,
    LucideTag,
  ],
  template: `
    <app-task-list-filter-action
      label="Filter by Tag"
      [icon]="lucideTag"
      [color]="selectedCount() ? 'primary' : undefined"
      (action)="onClicked(); menu.toggle(el.nativeElement)" />

    <app-dropdown-menu #menu>
      @if (loaded()) {
        @if (tags().length) {
          @for (tag of tags(); track tag.id) {
            <button
              app-menu-checkbox-item
              [checked]="tag.selected ?? false"
              (checkedChange)="onOptionClicked(tag)">
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
export class TaskListTagsComponent {
  private readonly store = inject(Store);
  readonly el = inject(ElementRef);

  readonly lucideTag = LucideTag;

  readonly tags = this.store.selectSignal(TagSelectors.selectTasksWithSelect);
  readonly loaded = this.store.selectSignal(TagSelectors.selectTagsLoaded);
  readonly selectedCount = this.store.selectSignal(
    TagSelectors.selectSelectedTagCount
  );

  onClicked() {
    this.store.dispatch(TagActions.loadTags());
  }

  onOptionClicked(selected: Selected<Tag>) {
    this.store.dispatch(TagActions.toggleSelectedTag({ tag: selected.name }));
  }
}
