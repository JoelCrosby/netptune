import { Component, inject } from '@angular/core';
import { Selected } from '@core/models/selected';
import { Tag } from '@core/models/tag';
import * as TagActions from '@core/store/tags/tags.actions';
import * as TagSelectors from '@core/store/tags/tags.selectors';
import { Store } from '@ngrx/store';
import { TagFilterComponent } from '@static/components/tag-filter/tag-filter.component';

@Component({
  selector: 'app-tag-filter-container',
  imports: [TagFilterComponent],
  template: `
    <app-tag-filter
      [tags]="tags()"
      [loaded]="loaded()"
      [selectedCount]="selectedCount()"
      (opened)="onOpened()"
      (toggled)="onToggled($event)" />
  `,
})
export class TagFilterContainerComponent {
  private readonly store = inject(Store);

  readonly tags = this.store.selectSignal(TagSelectors.selectTasksWithSelect);
  readonly loaded = this.store.selectSignal(TagSelectors.selectTagsLoaded);
  readonly selectedCount = this.store.selectSignal(
    TagSelectors.selectSelectedTagCount
  );

  onOpened() {
    this.store.dispatch(TagActions.loadTags.init());
  }

  onToggled(tag: Selected<Tag>) {
    this.store.dispatch(TagActions.toggleSelectedTag({ tag: tag.name }));
  }
}
