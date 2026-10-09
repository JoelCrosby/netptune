import { toggleValue } from '@core/util/arrays';
import { Component, computed, input, output } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { Selected } from '@core/models/selected';
import { Tag } from '@core/models/tag';
import { statusResource } from '@core/resources/status.resource';
import { tagResource } from '@core/resources/tag.resource';
import { userResource } from '@core/resources/user.resource';
import { LucideFlag } from '@lucide/angular';
import {
  AvatarFilterComponent,
  AvatarFilterOption,
} from '@static/components/avatar-filter/avatar-filter.component';
import { InlineButtonComponent } from '@static/components/button/inline-button.component';
import { FilterToggleComponent } from '@static/components/filter-toggle/filter-toggle.component';
import { SearchInputComponent } from '@static/components/search-input/search-input.component';
import { StatusFilterComponent } from '@static/components/status-filter/status-filter.component';
import { TagFilterComponent } from '@static/components/tag-filter/tag-filter.component';

@Component({
  selector: 'app-task-view-filters',
  imports: [
    AvatarFilterComponent,
    FilterToggleComponent,
    InlineButtonComponent,
    LucideFlag,
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
            i18n-emptyLabel="Shown when there are no members to filter by"
            emptyLabel="No members"
            [options]="assigneeOptions()"
            (optionClicked)="toggleAssignee($event)" />
        </div>
      }

      @if (supportsFlags() && readFlags()) {
        <div class="border-border border-l pl-3">
          <button
            app-filter-toggle
            [pressed]="flagged()"
            (click)="flaggedChanged.emit(!flagged())">
            <svg lucideFlag size="16" aria-hidden="true"></svg>
            <span
              i18n="Indicates a task has one or more flags raised against it">
              Flagged
            </span>
          </button>
        </div>
      }

      @if (tags.canRead()) {
        <div class="border-border border-l pl-3">
          <app-tag-filter
            [tags]="tagOptions()"
            [loaded]="!tags.isLoading()"
            [selectedCount]="tagNames().length"
            [allowUntagged]="supportsUntagged()"
            [untagged]="untagged()"
            (toggled)="toggleTag($event)"
            (untaggedChange)="untaggedChanged.emit($event)" />
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

      <ng-content />

      @if (hasFilters()) {
        <button
          app-inline-button
          color="muted"
          appearance="soft"
          class="ml-auto px-3 py-2 text-sm font-medium"
          (click)="cleared.emit()">
          <span i18n="Button that clears every active filter">
            Clear filters
          </span>
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
  readonly supportsFlags = input(false);
  readonly flagged = input(false);
  readonly supportsUntagged = input(false);
  readonly untagged = input(false);
  // Lets a caller with controls of its own in the content slot keep the
  // clear button visible while only those are active.
  readonly extraFiltersActive = input(false);

  readonly searchChanged = output<string | null>();
  readonly assigneeIdsChanged = output<string[]>();
  readonly tagNamesChanged = output<string[]>();
  readonly statusIdsChanged = output<number[]>();
  readonly flaggedChanged = output<boolean>();
  readonly untaggedChanged = output<boolean>();
  readonly cleared = output();

  readonly users = userResource();
  readonly tags = tagResource();
  readonly statuses = statusResource();
  readonly readFlags = hasPermission(PERMISSIONS.flags.read);

  readonly selectedStatuses = computed(() => new Set(this.statusIds()));
  readonly hasFilters = computed(
    () =>
      !!this.search() ||
      this.assigneeIds().length > 0 ||
      this.tagNames().length > 0 ||
      this.statusIds().length > 0 ||
      this.flagged() ||
      this.untagged() ||
      this.extraFiltersActive()
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
