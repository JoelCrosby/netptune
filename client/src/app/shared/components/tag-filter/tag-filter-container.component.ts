import { Component, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Selected } from '@core/models/selected';
import { Tag } from '@core/models/tag';
import { tagResource } from '@core/resources/tag.resource';
import { taskFilterRoute } from '@core/router/task-filter-route';
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
      (toggled)="onToggled($event)"
      (untaggedChange)="onUntaggedChange($event)" />
  `,
})
export class TagFilterContainerComponent {
  private readonly router = inject(Router);
  private readonly filterRoute = taskFilterRoute();
  private readonly tagsResource = tagResource();

  private readonly selected = computed(
    () => new Set(this.filterRoute.filters().tags ?? [])
  );

  readonly tags = computed<Selected<Tag>[]>(() => {
    const selected = this.selected();

    return this.tagsResource.value().map((tag) => ({
      ...tag,
      selected: selected.has(tag.name),
    }));
  });

  readonly loaded = computed(() => !this.tagsResource.isLoading());
  readonly selectedCount = computed(() => this.selected().size);

  readonly untagged = computed(
    () => this.filterRoute.filters().hasTags === false
  );

  onToggled(tag: Selected<Tag>) {
    const selected = new Set(this.selected());

    if (selected.has(tag.name)) {
      selected.delete(tag.name);
    } else {
      selected.add(tag.name);
    }

    this.filterRoute.set('tags', [...selected]);
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
