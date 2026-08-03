import { Component, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { injectParams } from '@app/core/router/signals';
import { parseTaskFilterRouteParams } from '@app/core/router/task-filter-route-params';
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
      [untagged]="untagged()"
      (opened)="onOpened()"
      (toggled)="onToggled($event)"
      (untaggedChange)="onUntaggedChange($event)" />
  `,
})
export class TagFilterContainerComponent {
  private readonly store = inject(Store);
  private readonly router = inject(Router);
  private readonly params = injectParams();

  readonly tags = this.store.selectSignal(TagSelectors.selectTasksWithSelect);
  readonly loaded = this.store.selectSignal(TagSelectors.selectTagsLoaded);
  readonly selectedCount = this.store.selectSignal(
    TagSelectors.selectSelectedTagCount
  );

  readonly untagged = computed(
    () => parseTaskFilterRouteParams(this.params()).hasTags === false
  );

  onOpened() {
    this.store.dispatch(TagActions.loadTags.init());
  }

  onToggled(tag: Selected<Tag>) {
    this.store.dispatch(TagActions.toggleSelectedTag({ tag: tag.name }));
  }

  onUntaggedChange(untagged: boolean) {
    void this.router.navigate([], {
      queryParams: {
        hasTags: untagged ? false : null,
      },
      queryParamsHandling: 'merge',
    });
  }
}
