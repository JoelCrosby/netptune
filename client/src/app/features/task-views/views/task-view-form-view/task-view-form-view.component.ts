import { Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { QueryBuilderGroup } from '@shared/components/query-builder/query-builder.models';
import { QueryChipBarComponent } from '@shared/components/query-builder/query-chip-bar.component';
import { LucideLink, LucideSave, LucideSettings2 } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DatatableColumnPreference } from '@static/components/datatable/datatable.types';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import {
  DEFAULT_VIEW_PAGE_SIZE,
  SaveTaskViewRequest,
  TaskQueryGroup,
  TaskViewResult,
  emptyQueryGroup,
} from '../../models/task-view.models';
import { emptyTaskQueryMessage } from '../../models/task-query-copy';
import {
  fromBuilderGroup,
  toBuilderGroup,
} from '../../models/task-query-builder';
import {
  taskQueryCatalogResource,
  taskViewResource,
} from '../../resources/task-view.resource';
import { QueryFieldOptionsService } from '../../services/query-field-options.service';
import { taskQueryPreviewResource } from '../../resources/task-query-preview.resource';
import { TaskViewsService } from '../../services/task-views.service';
import { decodeQuery, encodeQuery } from '../../util/query-url';
import {
  allTaskColumns,
  defaultTaskColumnPreferences,
  visibleTaskColumns,
} from '@core/tasks/task-columns';
import { TaskTableComponent } from '@static/components/task-table.component';
import { TaskViewDetailsDrawerComponent } from '../../components/task-view-details-drawer.component';
import { TaskViewPreviewToolbarComponent } from '../../components/task-view-preview-toolbar.component';

/**
 * One-column editor: query on top, results underneath. The split/stacked layout toggle is gone —
 * the query bar is short enough that the preview is always on screen — and the view's own details
 * live in a drawer rather than a permanent form.
 */
@Component({
  selector: 'app-task-view-form-view',
  imports: [
    PageBodyComponent,
    PageContainerComponent,
    PageHeaderComponent,
    PageLoadingComponent,
    QueryChipBarComponent,
    FlatButtonComponent,
    StrokedButtonComponent,
    TaskTableComponent,
    TaskViewDetailsDrawerComponent,
    TaskViewPreviewToolbarComponent,
    LucideLink,
    LucideSave,
    LucideSettings2,
  ],
  template: `
    <app-page-container layout="list" [centerPage]="false">
      <app-page-header
        toolbar
        [title]="name()"
        [titleEditable]="true"
        (titleSubmitted)="name.set($event)">
        <button
          pageHeaderActions
          app-stroked-button
          color="neutral"
          class="h-9 gap-2"
          type="button"
          [class.text-primary]="detailsOpen()"
          (click)="detailsOpen.set(!detailsOpen())">
          <svg lucideSettings2 class="h-4 w-4"></svg>
          <span i18n="Button that opens the view details drawer">Details</span>
        </button>

        <button
          pageHeaderActions
          app-stroked-button
          color="neutral"
          class="h-9 gap-2"
          type="button"
          [disabled]="!shareableQuery()"
          (click)="onCopyQueryLink()">
          <svg lucideLink class="h-4 w-4"></svg>
          <span i18n="Button that copies a link carrying the built query">
            Copy link
          </span>
        </button>

        <button
          pageHeaderActions
          app-flat-button
          color="primary"
          class="h-9 gap-2"
          type="button"
          [disabled]="!canSave()"
          (click)="onSave()">
          <svg lucideSave class="h-4 w-4"></svg>
          <span i18n="Button that saves a view">Save view</span>
        </button>
      </app-page-header>

      <app-page-body>
        @if (loading()) {
          <app-page-loading />
        } @else {
          @if (detailsOpen()) {
            <app-task-view-details-drawer
              class="shrink-0"
              [(description)]="description"
              [(isShared)]="isShared"
              [canManageShared]="canManageShared()"
              [savesAsCopy]="savesAsCopy()"
              (closed)="detailsOpen.set(false)" />
          }

          <div
            class="border-border bg-card shrink-0 rounded-t-xl border border-b-0 px-4 py-3.5">
            <app-query-chip-bar
              [group]="builderQuery()"
              [catalog]="builderCatalog()"
              [errors]="previewErrors()"
              i18n-summaryPrefix="Prefix of the plain-language query summary"
              summaryPrefix="Shows tasks where"
              [emptySummary]="emptySummary"
              (groupChange)="setQuery($event)" />
          </div>

          <app-task-view-preview-toolbar
            [loading]="previewLoading()"
            [count]="previewCount()"
            [availableColumns]="availableColumns"
            [sortableFields]="sortableFields()"
            [(preferences)]="columns"
            [(sortBy)]="sortBy"
            [(sortDirection)]="sortDirection" />

          <app-task-table
            class="mb-6"
            containerClass="rounded-t-none rounded-b-xl"
            tableClass="min-w-[860px] table-fixed"
            key="task-view-preview"
            i18n-emptyMessage="Shown when a query preview matches no tasks"
            emptyMessage="No tasks match this query yet."
            [fill]="true"
            [columns]="previewColumns()"
            [items]="previewRows"
            [loading]="previewLoading"
            [stickyHeader]="true" />
        }
      </app-page-body>
    </app-page-container>
  `,
})
export class TaskViewFormViewComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snackbar = inject(SnackbarService);
  private readonly service = inject(TaskViewsService);
  private readonly fieldOptions = inject(QueryFieldOptionsService);

  readonly availableColumns = allTaskColumns<TaskViewModel>();

  // The preview renders exactly what the saved view will, so it uses the same
  // catalog columns filtered to the ones the editor has switched on.
  readonly previewColumns = computed(() => {
    return visibleTaskColumns<TaskViewModel>(this.columns());
  });

  readonly detailsOpen = signal(false);

  private readonly routeParams = toSignal(this.route.params, {
    initialValue: {} as Params,
  });
  private readonly queryParams = toSignal(this.route.queryParams, {
    initialValue: {} as Params,
  });

  readonly viewSlug = computed<string | undefined>(() => {
    return this.routeParams()['slug'] || undefined;
  });

  private readonly viewRef = taskViewResource(this.viewSlug);
  private readonly catalogRef = taskQueryCatalogResource();

  readonly catalog = this.catalogRef.value;
  readonly loading = computed(() => {
    return this.catalogRef.isLoading() || this.viewRef.isLoading();
  });

  readonly name = signal(
    $localize`:Default name of a new saved task view:Untitled view`
  );
  readonly description = signal('');
  readonly isShared = signal(false);
  readonly query = signal<TaskQueryGroup>(emptyQueryGroup());

  readonly emptySummary = emptyTaskQueryMessage;

  // The chip bar speaks the shared builder vocabulary, so the view's own query crosses over on the
  // way in and back again on every edit.
  readonly builderCatalog = computed(() => {
    return this.fieldOptions.builderCatalog(this.catalog());
  });

  readonly builderQuery = computed(() => toBuilderGroup(this.query()));

  readonly columns = signal<DatatableColumnPreference[]>(
    defaultTaskColumnPreferences()
  );
  readonly sortBy = signal('');
  readonly sortDirection = signal('desc');

  readonly canManageShared = hasPermission(PERMISSIONS.taskViews.manageShared);

  // Opening somebody else's shared view without the rights to change it turns the editor into a
  // "save your own copy" flow rather than a form whose save button is guaranteed to be refused.
  readonly savesAsCopy = signal(false);

  readonly sortableFields = computed(() => {
    return this.catalog().fields.filter((field) => field.isSortable);
  });

  readonly canSave = computed(() => Boolean(this.name().trim()));

  readonly shareableQuery = computed(() => encodeQuery(this.query()));

  private readonly previewRequest = computed(() => {
    const query = this.query();
    const isEmpty = !query.conditions.length && !query.groups.length;

    if (isEmpty) return undefined;

    return {
      query,
      page: 1,
      pageSize: DEFAULT_VIEW_PAGE_SIZE,
      sortBy: this.sortBy() || null,
      sortDirection: this.sortDirection(),
    };
  });

  private readonly previewRef = taskQueryPreviewResource(this.previewRequest);

  readonly previewLoading = this.previewRef.isLoading;

  private readonly previewPayload = computed<TaskViewResult | undefined>(() => {
    return this.previewRef.value()?.payload;
  });

  readonly previewRows = computed<TaskViewModel[]>(() => {
    return this.previewPayload()?.items ?? [];
  });

  readonly previewCount = computed(() => {
    return this.previewPayload()?.totalCount ?? 0;
  });

  readonly previewErrors = computed(() => {
    return this.previewPayload()?.errors ?? [];
  });

  constructor() {
    effect(() => {
      const view = this.viewRef.value()?.payload;

      if (!view) return;

      const isCopy = !view.canEdit;

      this.name.set(isCopy ? copyName(view.name) : view.name);
      this.description.set(view.description ?? '');
      this.isShared.set(isCopy ? false : view.isShared);
      this.savesAsCopy.set(isCopy);
      this.query.set(view.definition?.query ?? emptyQueryGroup());
      this.sortBy.set(view.definition?.display.sortBy ?? '');
      this.sortDirection.set(view.definition?.display.sortDirection ?? 'desc');

      const saved = view.definition?.display.columns ?? [];

      this.columns.set(saved.length ? saved : defaultTaskColumnPreferences());
    });

    // A q parameter replaces the query outright, whether the editor was opened on a saved view or
    // on a blank one, so a link always shows the query it carries rather than merging into a saved one.
    effect(() => {
      const decoded = decodeQuery(this.queryParams()['q'] ?? null);

      if (decoded) {
        this.query.set(decoded);
      }
    });
  }

  setQuery(group: QueryBuilderGroup) {
    this.query.set(fromBuilderGroup(group));
  }

  onCopyQueryLink() {
    const encoded = this.shareableQuery();

    if (!encoded) {
      this.snackbar.warn(
        $localize`:Shown when a query is too large to share as a link:This query is too large to share as a link`
      );

      return;
    }

    // The link points at a blank editor rather than this view, so a colleague who cannot see the
    // saved view can still open the query it carries.
    const tree = this.router.createUrlTree(['../new'], {
      relativeTo: this.route,
      queryParams: { q: encoded },
    });
    const url = new URL(this.router.serializeUrl(tree), window.location.origin);

    void navigator.clipboard.writeText(url.toString()).then(
      () => {
        this.snackbar.success(
          $localize`:Confirmation that a query link was copied:Link copied`
        );
      },
      () => {
        this.snackbar.error(
          $localize`:Shown when a query link could not be copied:Link could not be copied`
        );
      }
    );
  }

  onSave() {
    const request: SaveTaskViewRequest = {
      id: this.savesAsCopy()
        ? null
        : (this.viewRef.value()?.payload?.id ?? null),
      name: this.name().trim(),
      description: this.description().trim() || null,
      isShared: this.isShared(),
      definition: {
        version: 1,
        query: this.query(),
        display: {
          columns: this.columns(),
          sortBy: this.sortBy() || null,
          sortDirection: this.sortDirection(),
          pageSize: DEFAULT_VIEW_PAGE_SIZE,
        },
      },
    };
    const save = request.id
      ? this.service.update(request)
      : this.service.create(request);

    save.subscribe({
      next: (view) => {
        this.snackbar.success(
          $localize`:Confirmation that a view was saved:View saved`
        );
        void this.router.navigate(['../', view.slug], {
          relativeTo: this.route,
        });
      },
      error: (error: Error) => {
        this.snackbar.error(error.message);
      },
    });
  }
}

function copyName(name: string): string {
  return $localize`:Default name for a copy of somebody else's view:${name}:viewName: (copy)`;
}
