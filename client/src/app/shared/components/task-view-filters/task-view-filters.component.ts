import { Component, computed, input, output } from '@angular/core';
import { Selected } from '@core/models/selected';
import { Tag } from '@core/models/tag';
import { statusResource } from '@core/resources/status.resources';
import { tagResource } from '@core/resources/tag.resource';
import { userResource } from '@core/resources/user.resource';
import {
  AvatarFilterComponent,
  AvatarFilterOption,
} from '@static/components/avatar-filter/avatar-filter.component';
import { SearchInputComponent } from '@static/components/search-input/search-input.component';
import { StatusFilterComponent } from '@static/components/status-filter/status-filter.component';
import { TagFilterComponent } from '@static/components/tag-filter/tag-filter.component';

@Component({
  selector: 'app-task-view-filters',
  imports: [
    AvatarFilterComponent,
    SearchInputComponent,
    StatusFilterComponent,
    TagFilterComponent,
  ],
  host: { class: 'block border-border border-b' },
  template: `
    <div class="flex min-h-14 flex-wrap items-center gap-3 px-3 py-2">
      <app-search-input
        [term]="search()"
        (searchChange)="searchChanged.emit($event)" />

      @if (users.canRead()) {
        <div class="border-border border-l pl-3">
          <app-avatar-filter
            emptyLabel="No members"
            [options]="assigneeOptions()"
            (optionClicked)="toggleAssignee($event)" />
        </div>
      }

      @if (tags.canRead()) {
        <div class="border-border border-l pl-3">
          <app-tag-filter
            [tags]="tagOptions()"
            [loaded]="!tags.isLoading()"
            [selectedCount]="tagNames().length"
            (toggled)="toggleTag($event)" />
        </div>
      }

      @if (statuses.canRead()) {
        <div class="border-border border-l pl-3">
          <app-status-filter
            [statuses]="statuses.value()"
            [selected]="selectedStatuses()"
            [selectedCount]="statusIds().length"
            (toggled)="toggleStatus($event)" />
        </div>
      }

      @if (hasFilters()) {
        <button
          type="button"
          class="text-muted-foreground hover:bg-muted hover:text-foreground ml-auto cursor-pointer rounded px-3 py-2 text-sm font-medium transition-colors"
          (click)="cleared.emit()">
          Clear filters
        </button>
      }
    </div>
  `,
})
export class TaskViewFiltersComponent {
  readonly search = input<string>();
  readonly assigneeIds = input<string[]>([]);
  readonly tagNames = input<string[]>([]);
  readonly statusIds = input<number[]>([]);

  readonly searchChanged = output<string | null>();
  readonly assigneeIdsChanged = output<string[]>();
  readonly tagNamesChanged = output<string[]>();
  readonly statusIdsChanged = output<number[]>();
  readonly cleared = output();

  readonly users = userResource();
  readonly tags = tagResource();
  readonly statuses = statusResource();

  readonly selectedStatuses = computed(() => new Set(this.statusIds()));
  readonly hasFilters = computed(
    () =>
      !!this.search() ||
      this.assigneeIds().length > 0 ||
      this.tagNames().length > 0 ||
      this.statusIds().length > 0
  );
  readonly assigneeOptions = computed<AvatarFilterOption[]>(() => {
    const selected = new Set(this.assigneeIds());
    return (this.users.value()?.payload?.items ?? []).map((user) => ({
      id: user.id,
      displayName: user.displayName,
      pictureUrl: user.pictureUrl,
      isServiceAccount: user.isServiceAccount,
      selected: selected.has(user.id),
    }));
  });
  readonly tagOptions = computed<Selected<Tag>[]>(() => {
    const selected = new Set(this.tagNames());
    return this.tags.value().map((tag) => ({
      ...tag,
      selected: selected.has(tag.name),
    }));
  });

  toggleAssignee(option: AvatarFilterOption): void {
    this.assigneeIdsChanged.emit(toggleValue(this.assigneeIds(), option.id));
  }

  toggleTag(tag: Selected<Tag>): void {
    this.tagNamesChanged.emit(toggleValue(this.tagNames(), tag.name));
  }

  toggleStatus(statusId: number): void {
    this.statusIdsChanged.emit(toggleValue(this.statusIds(), statusId));
  }
}

function toggleValue<T>(values: T[], value: T): T[] {
  return values.includes(value)
    ? values.filter((item) => item !== value)
    : [...values, value];
}
