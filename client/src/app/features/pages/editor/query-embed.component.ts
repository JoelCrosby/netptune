import { Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PERMISSIONS } from '@core/auth/permissions';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import {
  permissionResource,
  requestFrom,
} from '@core/resources/permission.resource';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { EmbedFrameComponent } from '@static/components/embed-frame/embed-frame.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { TaskCompactRowComponent } from '@static/components/task-compact-row.component';

const embedPageSize = 10;

interface QueryResult {
  items: TaskViewModel[];
  totalCount: number;
}

interface QuerySummary {
  name: string;
}

// The live task list a {{query:slug}} block shows, drawn from the saved query.
@Component({
  selector: 'app-query-embed',
  imports: [
    EmbedFrameComponent,
    TaskCompactRowComponent,
    EmptyStateComponent,
    RouterLink,
  ],
  template: `
    <app-embed-frame
      i18n-badge="Badge on an embedded list that updates as tasks change"
      badge="Live"
      [heading]="heading()"
      [link]="queryLink()"
      i18n-linkLabel="Link from an embedded query to the full query view"
      linkLabel="Open in Queries">
      @if (!result.canRead()) {
        <app-empty-state
          compact
          i18n-title="
            Shown on an embedded saved query the viewer may not see the tasks of
          "
          title="You don't have access to these tasks" />
      } @else if (result.error()) {
        <app-empty-state
          compact
          i18n-title="Shown when an embedded saved query cannot be loaded"
          title="This query couldn't be loaded"
          i18n-description="
            Hint shown when an embedded saved query cannot be loaded
          "
          description="It may have been deleted, or you may not have access to it." />
      } @else {
        @for (task of tasks(); track task.id) {
          <a class="block" [routerLink]="taskLink(task)">
            <app-task-compact-row [task]="task" />
          </a>
        } @empty {
          @if (!result.isLoading()) {
            <app-empty-state
              compact
              i18n-title="Shown when an embedded saved query matches no tasks"
              title="No tasks match this query" />
          }
        }
        @if (remaining() > 0) {
          <a class="block px-4 py-2" [routerLink]="queryLink()">
            <span
              i18n="Link under an embedded query to the tasks it does not show">
              and
              {{
                remaining() // i18n(ph="COUNT")
              }}
              more
            </span>
          </a>
        }
      }
    </app-embed-frame>
  `,
})
export class QueryEmbedComponent {
  private readonly workspaceSlug = inject(CurrentWorkspaceService).slug;

  readonly slug = input.required<string>();

  protected readonly summary = permissionResource<QuerySummary | undefined>({
    permission: PERMISSIONS.queries.read,
    request: requestFrom(this.slug, (slug) => ({
      url: `api/task-views/${slug}`,
    })),
    parse: (response) => response.payload,
  });

  protected readonly result = permissionResource<QueryResult | undefined>({
    permission: PERMISSIONS.tasks.read,
    request: requestFrom(this.slug, (slug) => ({
      url: `api/task-views/${slug}/tasks`,
      params: { page: 1, pageSize: embedPageSize },
    })),
    parse: (response) => response.payload,
    refreshOn: ['tasks'],
  });

  protected readonly heading = computed(
    () => this.summary.value()?.name ?? this.slug()
  );
  protected readonly tasks = computed(() => this.result.value()?.items ?? []);

  protected readonly remaining = computed(() => {
    const total = this.result.value()?.totalCount ?? 0;

    return Math.max(0, total - this.tasks().length);
  });

  protected readonly queryLink = computed(() => {
    return ['/', this.workspaceSlug() ?? '', 'queries', this.slug()];
  });

  protected taskLink(task: TaskViewModel): string[] {
    return ['/', this.workspaceSlug() ?? '', 'tasks', task.systemId];
  }
}
