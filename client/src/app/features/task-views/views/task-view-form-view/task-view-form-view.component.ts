import { Component, computed, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Params, Router } from '@angular/router';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { QueryChipBarComponent } from '@shared/components/query-builder/query-chip-bar.component';
import { LucideLink, LucideSave, LucideSettings2 } from '@lucide/angular';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { FormControlFieldComponent } from '@static/components/form-control/form-control-field.component';
import { FormControlLabelDirective } from '@static/components/form-control/form-control.directives';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DatatableColumnPreference } from '@static/components/datatable/datatable.types';
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
import {
  taskQueryCatalogResource,
  taskViewResource,
} from '../../resources/task-view.resource';
import { taskQueryPreviewResource } from '../../resources/task-query-preview.resource';
import { TaskViewsService } from '../../services/task-views.service';
import { decodeQuery, encodeQuery } from '../../util/query-url';
import {
  allTaskColumns,
  defaultTaskColumnPreferences,
  visibleTaskColumns,
} from '@core/tasks/task-columns';
import { TaskTableComponent } from '@static/components/task-table.component';
import { TaskViewDisplayMenuComponent } from '../../components/task-view-display-menu.component';

/**
 * One-column editor: query on top, results underneath. The split/stacked layout toggle is gone —
 * the query bar is short enough that the preview is always on screen — and the view's own details
 * live in a drawer rather than a permanent form.
 */
@Component({
  selector: 'app-task-view-form-view',
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    PageLoadingComponent,
    QueryChipBarComponent,
    CheckboxComponent,
    FormControlFieldComponent,
    FormControlLabelDirective,
    FormInputComponent,
    FlatButtonComponent,
    StrokedButtonComponent,
    TaskTableComponent,
    TaskViewDisplayMenuComponent,
    LucideLink,
    LucideSave,
    LucideSettings2,
  ],
  template: `
    <app-page-container [centerPage]="false" [fullHeight]="true">
      <app-page-header
        [title]="name()"
        [titleEditable]="true"
        (titleSubmitted)="name.set($event)">
        <div pageHeaderActions class="flex flex-wrap items-center gap-2">
          <button
            app-stroked-button
            color="neutral"
            class="h-9 gap-2"
            type="button"
            [class.text-primary]="detailsOpen()"
            (click)="detailsOpen.set(!detailsOpen())">
            <svg lucideSettings2 class="h-4 w-4"></svg>
            <span i18n="Button that opens the view details drawer"
              >Details</span
            >
          </button>

          <button
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
            app-flat-button
            color="primary"
            class="h-9 gap-2"
            type="button"
            [disabled]="!canSave()"
            (click)="onSave()">
            <svg lucideSave class="h-4 w-4"></svg>
            <span i18n="Button that saves a view">Save view</span>
          </button>
        </div>
      </app-page-header>

      @if (loading()) {
        <app-page-loading />
      } @else {
        @if (detailsOpen()) {
          <div
            class="border-border bg-card mb-3 grid items-end gap-4 rounded-xl border px-[18px] py-4 md:grid-cols-[1fr_1fr_auto]">
            <app-form-input
              density="compact"
              i18n-label="Label of the view description field"
              label="Description"
              name="view-description"
              [noMargin]="true"
              [(value)]="description" />

            <div>
              <span
                appFormLabel
                variant="compact"
                i18n="Label of the view visibility field">
                Visibility
              </span>

              <app-form-control-field density="compact">
                <app-checkbox
                  class="w-full px-3 text-sm"
                  [checked]="isShared()"
                  [disabled]="!canManageShared()"
                  (checkedChange)="isShared.set($event)">
                  <span
                    i18n="Checkbox that shares a view with the whole workspace">
                    Share with the workspace
                  </span>
                </app-checkbox>
              </app-form-control-field>
            </div>

            <button
              app-stroked-button
              color="neutral"
              class="h-9.5 rounded-lg"
              type="button"
              (click)="detailsOpen.set(false)">
              <span i18n="Button that closes the view details drawer"
                >Done</span
              >
            </button>
          </div>

          @if (!canManageShared()) {
            <p class="text-foreground/50 mb-3 text-xs">
              <span
                i18n="
                  Explains why the share control is unavailable to this user
                ">
                Sharing a view with the workspace needs the shared-views
                permission.
              </span>
            </p>
          }

          @if (savesAsCopy()) {
            <div
              class="border-primary/40 bg-primary/5 mb-3 rounded-md border px-3 py-2 text-sm"
              role="status">
              <span
                i18n="Shown when editing a shared view the user cannot change">
                You cannot change this shared view, so saving creates your own
                copy of it.
              </span>
            </div>
          }
        }

        <div
          class="border-border bg-card shrink-0 rounded-t-xl border border-b-0 px-4 py-3.5">
          <app-query-chip-bar
            [group]="query()"
            [catalog]="catalog()"
            [errors]="previewErrors()"
            (groupChange)="query.set($event)" />
        </div>

        <div
          class="border-border bg-card-header flex shrink-0 items-center gap-3 border-x border-t px-4 py-2.5">
          <p class="text-sm" role="status" aria-live="polite">
            @if (previewLoading()) {
              <span
                class="text-foreground/38"
                i18n="Shown while the query result count is being recounted">
                Counting…
              </span>
            } @else {
              <span class="text-foreground font-medium">
                {{ previewCount() }}
              </span>
              <span
                class="text-foreground/38"
                i18n="Label after the number of tasks a query matches">
                matching tasks
              </span>
            }
          </p>

          <app-task-view-display-menu
            class="ml-auto"
            [columns]="availableColumns"
            [preferences]="columns()"
            [sortableFields]="sortableFields()"
            [sortBy]="sortBy()"
            [sortDirection]="sortDirection()"
            (preferencesChange)="columns.set($event)"
            (sortByChange)="sortBy.set($event)"
            (sortDirectionChange)="sortDirection.set($event)" />
        </div>

        <app-task-table
          class="mb-6"
          containerClass="rounded-t-none rounded-b-xl"
          key="task-view-preview"
          i18n-emptyMessage="Shown when a query preview matches no tasks"
          emptyMessage="No tasks match this query yet."
          [fill]="true"
          [columns]="previewColumns()"
          [items]="previewRows"
          [loading]="previewLoading"
          [stickyHeader]="true" />
      }
    </app-page-container>
  `,
})
export class TaskViewFormViewComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snackbar = inject(SnackbarService);
  private readonly service = inject(TaskViewsService);

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
