import { Component, computed, viewChildren } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { Params } from '@angular/router';
import { PERMISSIONS } from '@core/auth/permissions';
import { SprintStatus } from '@core/enums/sprint-status';
import { Selected } from '@core/models/selected';
import { StatusCategory } from '@core/models/status';
import { AssigneeViewModel } from '@core/models/view-models/board-view';
import { sprintResource } from '@core/resources/sprint.resource';
import { taskFilterRoute } from '@core/router/task-filter-route';
import { workspaceUsersResource } from '@core/resources/user.resource';
import { TaskListFiltersComponent } from '@project-tasks/components/task-list/task-list-filters.component';
import { LucideCalendarPlus, LucideListChecks } from '@lucide/angular';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { IconTileComponent } from '@static/components/icon-tile.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SprintBacklogGroupComponent } from '../../components/sprint-backlog-group.component';

interface BacklogGroupConfig {
  label: string;
  categories: StatusCategory[];
}

@Component({
  selector: 'app-sprint-backlog-view',
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    EmptyStateComponent,
    IconTileComponent,
    LucideListChecks,
    TaskListFiltersComponent,
    SprintBacklogGroupComponent,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header
        i18n-title="Page title for the sprint backlog"
        title="Backlog"
        [count]="totalCount()" />

      <div class="flex flex-col gap-6">
        <app-task-list-filters [assigneeOptions]="assigneeOptions()" />

        @if (canManageTasks() && assignableSprints().length === 0) {
          <div
            class="border-border bg-card flex items-start gap-3 rounded-lg border border-dashed px-6 py-5 shadow-sm">
            <app-icon-tile [icon]="noticeIcon" />
            <p class="text-muted text-sm">
              <span
                i18n="
                  Shown when there is no sprint available to assign backlog
                  tasks to
                ">
                No planning or active sprints found. Create a sprint first to
                assign tasks to it.
              </span>
            </p>
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
          <div
            class="border-border bg-card rounded-lg border px-6 py-5 shadow-sm">
            <app-empty-state compact [title]="emptyMessage()">
              <svg emptyStateIcon lucideListChecks class="h-8 w-8"></svg>
            </app-empty-state>
          </div>
        }
      </div>
    </app-page-container>
  `,
})
export class SprintBacklogViewComponent {
  protected readonly noticeIcon = LucideCalendarPlus;

  protected readonly emptyMessage = computed(() => {
    return this.filtersActive()
      ? $localize`:Shown when no backlog task matches the active filters:No backlog tasks match these filters.`
      : $localize`:Shown when every task already belongs to a sprint:The backlog is empty — all tasks are assigned to sprints.`;
  });

  private readonly sprintsResource = sprintResource([]);
  readonly allSprints = this.sprintsResource.value;
  readonly users = workspaceUsersResource();
  private readonly filterRoute = taskFilterRoute();

  readonly searchTerm = computed(() => this.filterRoute.filters().term);
  readonly selectedTags = computed(() => this.filterRoute.filters().tags ?? []);
  readonly selectedStatuses = computed(
    () => this.filterRoute.filters().statuses ?? []
  );
  readonly selectedAssignees = computed(
    () => this.filterRoute.filters().users ?? []
  );
  readonly filtersActive = this.filterRoute.hasFilters;
  readonly canManageTasks = hasPermission(PERMISSIONS.sprints.manageTasks);

  private backlogGroups = viewChildren(SprintBacklogGroupComponent);

  readonly groups: BacklogGroupConfig[] = [
    {
      label: $localize`:Backlog group heading for tasks not started:New`,
      categories: [StatusCategory.todo],
    },
    {
      label: $localize`:Backlog group heading for tasks being worked on:In Progress`,
      categories: [StatusCategory.active],
    },
    {
      label: $localize`:Backlog group heading for tasks in any other status:Other`,
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

  readonly allEmpty = computed(() => {
    const groups = this.backlogGroups();

    if (groups.length === 0) return false;

    return groups.every((group) => group.hasLoaded() && group.count() === 0);
  });

  readonly totalCount = computed((): number | null => {
    const groups = this.backlogGroups();

    if (groups.length === 0 || !groups.every((group) => group.hasLoaded())) {
      return null;
    }

    return groups.reduce((total, group) => total + group.count(), 0);
  });
}
