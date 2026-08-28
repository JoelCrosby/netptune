import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Params, Router, RouterLink } from '@angular/router';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { taskNameCell, visibleTaskColumns } from '@core/tasks/task-columns';
import {
  LucideLink,
  LucidePencil,
  LucidePin,
  LucidePinOff,
  LucideTriangleAlert,
} from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CalloutComponent } from '@static/components/callout/callout.component';
import { DatatableColumn } from '@static/components/datatable/datatable.types';
import { TaskTableComponent } from '@static/components/task-table.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { QueryFieldOptionsService } from '../../services/query-field-options.service';
import { PinnedViewsService } from '../../services/pinned-views.service';
import { TaskQueryValidationError } from '../../models/task-view.models';
import {
  taskQueryCatalogResource,
  taskViewResource,
} from '../../resources/task-view.resource';
import { findStaleReferences } from '../../util/stale-references';

@Component({
  selector: 'app-task-view-detail-view',
  imports: [
    RouterLink,
    PageBodyComponent,
    PageContainerComponent,
    PageHeaderComponent,
    PageLoadingComponent,
    ErrorStateComponent,
    TaskTableComponent,
    CalloutComponent,
    FlatButtonComponent,
    StrokedButtonComponent,
    LucideLink,
    LucidePencil,
    LucidePin,
    LucidePinOff,
  ],
  template: `
    <app-page-container layout="list">
      @if (view(); as view) {
        <app-page-header toolbar [title]="view.name" [count]="totalCount()">
          <button
            pageHeaderActions
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
            pageHeaderActions
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
              pageHeaderActions
              app-flat-button
              color="primary"
              class="gap-2"
              [routerLink]="['edit']">
              <svg lucidePencil class="h-4 w-4"></svg>
              <span i18n="Button that opens the edit-view form">Edit</span>
            </a>
          }
        </app-page-header>
      }

      <app-page-body>
        @if (loading()) {
          <app-page-loading />
        } @else if (notFound()) {
          <app-error-state
            i18n-title="Shown when a saved view cannot be found"
            title="This view could not be found"
            i18n-description="Advice shown when a saved view is missing"
            description="It may have been deleted, or it may be private to somebody else." />
        } @else if (view(); as view) {
          <div class="mb-4 flex shrink-0 flex-col gap-2">
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
              <app-callout
                color="warn"
                role="alert"
                [icon]="warningIcon"
                i18n-title="Heading shown when a saved view no longer compiles"
                title="This view needs attention">
                <ul class="text-foreground/70 mt-1 list-disc pl-4">
                  @for (error of errors(); track error.path) {
                    <li>{{ error.message }}</li>
                  }
                </ul>
              </app-callout>
            }
          </div>

          <app-task-table
            i18n-itemLabel="Plural noun for tasks, used in the row summary"
            itemLabel="tasks"
            i18n-emptyMessage="Shown when a saved view matches no tasks"
            emptyMessage="No tasks match this view."
            tableClass="min-w-[860px]"
            [key]="tableKey()"
            [url]="tableUrl()"
            [params]="params"
            [columns]="columns()"
            [autoFill]="true"
            [stickyHeader]="true"
            [customizableColumns]="true"
            (loaded)="onLoaded($event)" />
        }
      </app-page-body>
    </app-page-container>
  `,
})
export class TaskViewDetailViewComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snackbar = inject(SnackbarService);
  private readonly fieldOptions = inject(QueryFieldOptionsService);
  private readonly pinned = inject(PinnedViewsService);

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

  protected readonly warningIcon = LucideTriangleAlert;

  readonly summary = computed(() => {
    const query = this.view()?.definition?.query;

    if (!query) return '';

    return this.fieldOptions.explain(query, this.catalogRef.value());
  });

  readonly params = computed(() => {
    const display = this.view()?.definition?.display;
    const sortBy = display?.sortBy ?? undefined;
    const sortDirection = display?.sortDirection ?? undefined;

    return { sortBy, sortDirection };
  });

  readonly tableKey = computed(() => `task-view-${this.viewSlug()}`);

  readonly tableUrl = computed(() => {
    return `api/task-views/${this.viewSlug()}/tasks`;
  });

  readonly columns = computed<DatatableColumn<TaskViewModel>[]>(() => {
    return visibleTaskColumns<TaskViewModel>(
      this.view()?.definition?.display.columns ?? [],
      {
        overrides: {
          name: taskNameCell<TaskViewModel>({
            link: (task) => ['/', task.workspaceKey, 'tasks', task.systemId],
          }),
        },
      }
    );
  });

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
