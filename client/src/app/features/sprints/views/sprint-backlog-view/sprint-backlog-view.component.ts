import { Component, computed, inject, viewChildren } from '@angular/core';
import { Params } from '@angular/router';
import { SprintStatus } from '@core/enums/sprint-status';
import { Selected } from '@core/models/selected';
import { StatusCategory } from '@core/models/status';
import { AssigneeViewModel } from '@core/models/view-models/board-view';
import { initBacklogView } from '@core/store/sprints/sprints.actions';
import { selectAllSprints } from '@core/store/sprints/sprints.selectors';
import {
  selectSelectedAssignees,
  selectSelectedTaskStatuses,
  selectTaskFiltersActive,
  selectTaskSearchTerm,
} from '@core/store/tasks/tasks.selectors';
import { selectSelectedTags } from '@core/store/tags/tags.selectors';
import { loadUsers } from '@core/store/users/users.actions';
import { selectAllUsers } from '@core/store/users/users.selectors';
import { dispatchForWorkspace } from '@core/util/dispatch-for-workspace';
import { Store } from '@ngrx/store';
import { TaskListFiltersComponent } from '@project-tasks/components/task-list/task-list-filters.component';
import { CardComponent } from '@static/components/card/card.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SprintBacklogGroupComponent } from '../../components/sprint-backlog-group.component';

interface BacklogGroupConfig {
  label: string;
  categories: StatusCategory[];
}

@Component({
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    CardComponent,
    TaskListFiltersComponent,
    SprintBacklogGroupComponent,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header title="Backlog" [count]="totalCount()" />

      <div class="flex flex-col gap-6">
        <app-task-list-filters [assigneeOptions]="assigneeOptions()" />

        @if (assignableSprints().length === 0) {
          <div
            class="text-muted border-border rounded border-2 border-dashed p-4 text-sm">
            No planning or active sprints found. Create a sprint first to assign
            tasks to it.
          </div>
        }

        @for (group of groups; track group.label) {
          <app-sprint-backlog-group
            [label]="group.label"
            [categories]="group.categories"
            [filterParams]="filterParams()"
            [sprints]="assignableSprints()" />
        }

        @if (allEmpty()) {
          <app-card class="text-muted min-h-0 text-center">
            {{
              filtersActive()
                ? 'No backlog tasks match these filters.'
                : 'The backlog is empty — all tasks are assigned to sprints.'
            }}
          </app-card>
        }
      </div>
    </app-page-container>
  `,
})
export class SprintBacklogViewComponent {
  private store = inject(Store);

  readonly allSprints = this.store.selectSignal(selectAllSprints);
  readonly users = this.store.selectSignal(selectAllUsers);
  readonly searchTerm = this.store.selectSignal(selectTaskSearchTerm);
  readonly selectedTags = this.store.selectSignal(selectSelectedTags);
  readonly selectedStatuses = this.store.selectSignal(selectSelectedTaskStatuses);
  readonly selectedAssignees = this.store.selectSignal(selectSelectedAssignees);
  readonly filtersActive = this.store.selectSignal(selectTaskFiltersActive);

  private backlogGroups = viewChildren(SprintBacklogGroupComponent);

  readonly groups: BacklogGroupConfig[] = [
    { label: 'New', categories: [StatusCategory.todo] },
    { label: 'In Progress', categories: [StatusCategory.active] },
    {
      label: 'Other',
      categories: [
        StatusCategory.backlog,
        StatusCategory.done,
        StatusCategory.inactive,
      ],
    },
  ];

  readonly assignableSprints = computed(() =>
    this.allSprints().filter(
      (s) =>
        s.status === SprintStatus.planning || s.status === SprintStatus.active
    )
  );

  // Assignee filter options come from the workspace member list rather than the
  // tasks currently paged into view, so the filter is complete and stable.
  readonly assigneeOptions = computed((): Selected<AssigneeViewModel>[] => {
    const selectedSet = new Set(this.selectedAssignees());

    return this.users()
      .map((user) => ({
        id: user.id,
        displayName: user.displayName,
        pictureUrl: user.pictureUrl ?? '',
        selected: selectedSet.has(user.id),
      }))
      .sort((a, b) => a.displayName.localeCompare(b.displayName));
  });

  // Shared query params (search/tags/status/assignees) applied to every group's
  // datatable fetch; each group adds its own status categories on top.
  readonly filterParams = computed((): Params => {
    const params: Params = {};
    const search = this.searchTerm()?.trim();

    if (search) {
      params['search'] = search;
    }

    const tags = this.selectedTags();
    if (tags.length) {
      params['tags'] = tags;
    }

    const statuses = this.selectedStatuses();
    if (statuses.length) {
      params['statusIds'] = statuses;
    }

    const assignees = this.selectedAssignees();
    if (assignees.length) {
      params['assignees'] = assignees;
    }

    return params;
  });

  // Only show the global empty message once every group has resolved its fetch
  // and all came back empty.
  readonly allEmpty = computed(() => {
    const groups = this.backlogGroups();

    if (groups.length === 0) return false;

    return groups.every((group) => group.hasLoaded() && group.count() === 0);
  });

  // Combined backlog total for the page header, available once every group has
  // resolved its fetch.
  readonly totalCount = computed((): number | null => {
    const groups = this.backlogGroups();

    if (groups.length === 0 || !groups.every((group) => group.hasLoaded())) {
      return null;
    }

    return groups.reduce((total, group) => total + group.count(), 0);
  });

  constructor() {
    dispatchForWorkspace(() => initBacklogView());
    dispatchForWorkspace(() => loadUsers.init());
  }
}
