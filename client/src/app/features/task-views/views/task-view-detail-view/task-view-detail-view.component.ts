import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Params, Router, RouterLink } from '@angular/router';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { ProjectTasksHubService } from '@core/services/tasks-hub.service';
import { colorSwatchClass } from '@core/util/colors/colors';
import { TaskPriority, taskPriorityLabels } from '@core/enums/task-priority';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import {
  LucideLink,
  LucidePencil,
  LucidePin,
  LucidePinOff,
  LucideTriangleAlert,
} from '@lucide/angular';
import { AvatarStackComponent } from '@static/components/avatar-stack/avatar-stack.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import {
  DatatableColumn,
  DatatableDataSource,
} from '@static/components/datatable/datatable.types';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { QueryFieldOptionsService } from '../../services/query-field-options.service';
import { PinnedViewsService } from '../../services/pinned-views.service';
import {
  TaskQueryValidationError,
  TaskViewResult,
} from '../../models/task-view.models';
import {
  taskQueryCatalogResource,
  taskViewResource,
} from '../../resources/task-view.resource';
import { taskViewColumns } from '../../util/task-view-columns';
import { findStaleReferences } from '../../util/stale-references';
import { explainQuery } from '../../util/query-explanation';

@Component({
  selector: 'app-task-view-detail-view',
  imports: [
    RouterLink,
    DatePipe,
    AvatarStackComponent,
    PageContainerComponent,
    PageHeaderComponent,
    PageLoadingComponent,
    ErrorStateComponent,
    DatatableComponent,
    DatatableCellTemplateDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
    LucideLink,
    LucidePencil,
    LucidePin,
    LucidePinOff,
    LucideTriangleAlert,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      @if (loading()) {
        <app-page-loading />
      } @else if (notFound()) {
        <app-error-state
          i18n-title="Shown when a saved view cannot be found"
          title="This view could not be found"
          i18n-description="Advice shown when a saved view is missing"
          description="It may have been deleted, or it may be private to somebody else." />
      } @else if (view(); as view) {
        <app-page-header [title]="view.name" [count]="totalCount()">
          <div pageHeaderActions class="flex flex-wrap items-center gap-2">
            <button
              app-stroked-button
              class="gap-2"
              type="button"
              (click)="onCopyLink()">
              <svg lucideLink class="h-4 w-4"></svg>
              <span i18n="Button that copies a shareable link to a view">
                Copy link
              </span>
            </button>

            <button
              app-stroked-button
              class="gap-2"
              type="button"
              [attr.aria-pressed]="isPinned()"
              (click)="onTogglePin()">
              @if (isPinned()) {
                <svg lucidePin class="text-primary h-4 w-4"></svg>
                <span i18n="Button that removes a view from the sidebar">
                  Unpin
                </span>
              } @else {
                <svg lucidePinOff class="h-4 w-4"></svg>
                <span i18n="Button that adds a view to the sidebar">Pin</span>
              }
            </button>

            @if (view.canEdit && canUpdate()) {
              <a
                app-flat-button
                color="primary"
                class="gap-2"
                [routerLink]="['edit']">
                <svg lucidePencil class="h-4 w-4"></svg>
                <span i18n="Button that opens the edit-view form">Edit</span>
              </a>
            }
          </div>
        </app-page-header>

        <div class="flex flex-col gap-4">
          @if (view.description) {
            <p class="text-foreground/60 text-sm">{{ view.description }}</p>
          }

          <p class="text-foreground/50 text-sm">
            <span
              class="font-medium"
              i18n="Prefix of the plain-language query summary">
              Shows tasks where
            </span>
            {{ summary() }}
          </p>

          @if (errors().length) {
            <div
              class="border-warn/40 bg-warn/5 flex items-start gap-2 rounded-md border px-3 py-2 text-sm"
              role="alert">
              <svg
                lucideTriangleAlert
                class="text-warn mt-0.5 h-4 w-4 shrink-0"
                aria-hidden="true"></svg>
              <div class="min-w-0">
                <p
                  class="font-medium"
                  i18n="Heading shown when a saved view no longer compiles">
                  This view needs attention
                </p>
                <ul class="text-foreground/70 mt-1 list-disc pl-4">
                  @for (error of errors(); track error.path) {
                    <li>{{ error.message }}</li>
                  }
                </ul>
              </div>
            </div>
          }

          <app-datatable
            containerClass="overflow-auto rounded-lg shadow-sm"
            tableClass="min-w-[860px]"
            headerClass="bg-card-header text-muted uppercase"
            i18n-itemLabel="Plural noun for tasks, used in the row summary"
            itemLabel="tasks"
            i18n-emptyMessage="Shown when a saved view matches no tasks"
            emptyMessage="No tasks match this view."
            [customizableColumns]="true"
            [data]="data()"
            (loaded)="onLoaded($event)">
            <ng-template appDatatableCell="systemId" let-task>
              <span class="text-muted font-mono">{{ task.systemId }}</span>
            </ng-template>

            <ng-template appDatatableCell="name" let-task>
              <a
                class="font-medium hover:underline"
                [routerLink]="['/', task.workspaceKey, 'tasks', task.systemId]">
                {{ task.name }}
              </a>
            </ng-template>

            <ng-template appDatatableCell="status" let-task>
              <span class="inline-flex items-center gap-1.5">
                <span
                  [class]="
                    'h-2 w-2 rounded-full ' + colorSwatchClass(task.statusColor)
                  "></span>
                <span>{{ task.statusName }}</span>
              </span>
            </ng-template>

            <ng-template appDatatableCell="assignees" let-task>
              @if (task.assignees.length) {
                <app-avatar-stack [avatars]="task.assignees" />
              } @else {
                <span
                  class="text-muted"
                  i18n="Shown when a task has nobody assigned">
                  Unassigned
                </span>
              }
            </ng-template>

            <ng-template appDatatableCell="priority" let-task>
              <span>{{ priorityLabel(task.priority) }}</span>
            </ng-template>

            <ng-template appDatatableCell="dueDate" let-task>
              <span class="whitespace-nowrap">
                {{ task.dueDate | date: 'mediumDate' }}
              </span>
            </ng-template>

            <ng-template appDatatableCell="startDate" let-task>
              <span class="whitespace-nowrap">
                {{ task.startDate | date: 'mediumDate' }}
              </span>
            </ng-template>

            <ng-template appDatatableCell="updatedAt" let-task>
              <span class="whitespace-nowrap">
                {{ task.updatedAt | date: 'mediumDate' }}
              </span>
            </ng-template>
          </app-datatable>
        </div>
      }
    </app-page-container>
  `,
})
export class TaskViewDetailViewComponent {
  readonly colorSwatchClass = colorSwatchClass;

  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snackbar = inject(SnackbarService);
  private readonly fieldOptions = inject(QueryFieldOptionsService);
  private readonly pinned = inject(PinnedViewsService);
  private readonly hub = inject(ProjectTasksHubService);

  private readonly routeParams = toSignal(this.route.params, {
    initialValue: {} as Params,
  });

  readonly viewSlug = computed<string | undefined>(() => {
    return this.routeParams()['slug'] || undefined;
  });

  private readonly viewRef = taskViewResource(this.viewSlug);
  private readonly catalogRef = taskQueryCatalogResource();

  readonly view = computed(() => this.viewRef.value()?.payload);
  readonly loading = computed(() => this.viewRef.isLoading());
  readonly notFound = computed(() => {
    return !this.loading() && !this.view();
  });

  readonly totalCount = signal<number | null>(null);

  readonly errors = computed<TaskQueryValidationError[]>(() => {
    const query = this.view()?.definition?.query;

    if (!query) return [];

    return findStaleReferences(
      query,
      this.catalogRef.value(),
      this.fieldOptions
    );
  });

  readonly canUpdate = hasPermission(PERMISSIONS.taskViews.update);

  readonly summary = computed(() => {
    const query = this.view()?.definition?.query;

    if (!query) return '';

    const catalog = this.catalogRef.value();

    return explainQuery(query, {
      catalog,
      labelFor: (fieldKey, value) => {
        const field = catalog.fields.find(
          (candidate) => candidate.key === fieldKey
        );

        return this.fieldOptions.labelFor(field, value);
      },
    });
  });

  private readonly params = computed(() => {
    const display = this.view()?.definition?.display;
    const sortBy = display?.sortBy ?? undefined;
    const sortDirection = display?.sortDirection ?? undefined;

    return { sortBy, sortDirection };
  });

  readonly data = computed<DatatableDataSource<TaskViewModel>>(() => {
    const slug = this.viewSlug();

    return {
      key: `task-view-${slug}`,
      columns: this.columns(),
      resource: {
        url: `api/task-views/${slug}/tasks`,
        params: this.params,
      },
      rows: (response) => {
        const payload = response?.payload as TaskViewResult | undefined;

        return payload?.items ?? [];
      },
      trackBy: (_: number, task: TaskViewModel) => task.id,
      reloadSignal: this.hub.updateVersion,
    };
  });

  private readonly columns = computed<DatatableColumn<TaskViewModel>[]>(() => {
    return taskViewColumns(this.view()?.definition?.display.columns ?? []);
  });

  priorityLabel(priority: TaskPriority | null | undefined): string {
    if (priority === null || priority === undefined) return '';

    return taskPriorityLabels[priority];
  }

  isPinned(): boolean {
    const id = this.view()?.id;

    return id !== undefined && this.pinned.isPinned(id);
  }

  onTogglePin() {
    const id = this.view()?.id;

    if (id === undefined) return;

    this.pinned.toggle(id);
  }

  onLoaded(event: { totalCount: number; hasValue: boolean }) {
    this.totalCount.set(event.totalCount);
  }

  onCopyLink() {
    const url = new URL(this.router.url, window.location.origin);

    void navigator.clipboard.writeText(url.toString()).then(
      () => {
        this.snackbar.success(
          $localize`:Confirmation that a shareable view link was copied:Link copied`
        );
      },
      () => {
        this.snackbar.error(
          $localize`:Shown when a shareable view link could not be copied:Link could not be copied`
        );
      }
    );
  }
}
