import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
} from '@angular/core';
import { Selected } from '@core/models/selected';
import { Tag } from '@core/models/tag';
import * as TagActions from '@core/store/tags/tags.actions';
import * as TagSelectors from '@core/store/tags/tags.selectors';
import { Store } from '@ngrx/store';
import { BoardGroupHeaderActionComponent } from '../board-group-header/board-group-header-action.component';
import { LucideTag } from '@lucide/angular';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuCheckboxItemComponent } from '@static/components/dropdown-menu/menu-checkbox-item.component';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';

@Component({
  selector: 'app-board-group-tags',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    BoardGroupHeaderActionComponent,
    DropdownMenuComponent,
    MenuCheckboxItemComponent,
    SpinnerComponent,
  ],
  template: `
    <app-board-group-header-action
      label="Filter by Tag"
      [icon]="lucideTag"
      (action)="onClicked(); menu.toggle(el.nativeElement)"
      [color]="selectedCount() ? 'primary' : undefined" />

    <app-dropdown-menu #menu>
      @if (loaded()) {
        @if (tags().length) {
          @for (tag of tags(); track trackByTag($index, tag)) {
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
export class BoardGroupTagsComponent {
  private store = inject(Store);
  readonly el = inject(ElementRef);

  lucideTag = LucideTag;

  tags = this.store.selectSignal(TagSelectors.selectTasksWithSelect);
  loaded = this.store.selectSignal(TagSelectors.selectTagsLoaded);
  selectedCount = this.store.selectSignal(TagSelectors.selectSelectedTagCount);

  trackByTag(_: number, tag: Selected<Tag>) {
    return tag.id;
  }

  onClicked() {
    this.store.dispatch(TagActions.loadTags());
  }

  onOptionClicked(selected: Selected<Tag>) {
    this.store.dispatch(TagActions.toggleSelectedTag({ tag: selected.name }));
  }
}
