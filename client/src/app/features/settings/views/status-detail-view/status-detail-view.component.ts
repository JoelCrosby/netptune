import {
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import {
  apply,
  disabled,
  FormField,
  form,
  maxLength,
  required,
  submit,
} from '@angular/forms/signals';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PERMISSONS } from '@core/auth/permissions';
import { ConfirmationService } from '@core/services/confirmation.service';
import { statusUsageResource } from '@core/resources/entity-usage.resource';
import { statusResource } from '@core/resources/status.resources';
import {
  Status,
  StatusCategory,
  statusCategoryLabels,
  statusCategoryOptions,
} from '@core/models/status';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { StatusesService } from '@core/services/statuses.service';
import { fallbackColor } from '@core/util/colors/colors';
import { requiredTextSchema } from '@core/util/forms/validation.schemas';
import {
  LucideListChecks,
  LucideSettings2,
  LucideShapes,
  LucideTrash2,
} from '@lucide/angular';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { ColorSelectComponent } from '@static/components/color-select/color-select.component';
import { ColorSwatchComponent } from '@static/components/color-swatch/color-swatch.component';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableEmptyDirective } from '@static/components/datatable/datatable-empty.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import { DatatableDataSource } from '@static/components/datatable/datatable.types';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';
import { PanelComponent } from '@static/components/panel.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { EntityUsagePanelComponent } from '../../components/entity-usage-panel.component';
import { EMPTY, finalize, firstValueFrom, switchMap } from 'rxjs';

@Component({
  selector: 'app-status-detail-view',
  imports: [
    AvatarComponent,
    BadgeComponent,
    ColorSelectComponent,
    ColorSwatchComponent,
    DatatableCellTemplateDirective,
    DatatableComponent,
    DatatableEmptyDirective,
    EmptyStateComponent,
    EntityUsagePanelComponent,
    ErrorStateComponent,
    FlatButtonComponent,
    FormField,
    FormInputComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    FormTextAreaComponent,
    LucideListChecks,
    LucideTrash2,
    PageContainerComponent,
    PageHeaderComponent,
    PanelComponent,
    PanelHeaderComponent,
    RouterLink,
    StrokedButtonComponent,
    TaskScopeIdComponent,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header
        i18n-title="Page title for a single workspace status"
        title="Status" />

      <div class="flex flex-col gap-6">
        @if (usage.error() || listError()) {
          <app-error-state
            compact
            i18n-title="Shown when a status detail page fails to load"
            title="Status could not be loaded"
            i18n-description="Advice shown when a list fails to load"
            description="Check your connection and try again."
            (retry)="reload()" />
        } @else {
          <app-panel>
            <app-panel-header
              i18n-heading="Heading above the status edit form"
              heading="Status details"
              i18n-description="Description of the status edit form"
              description="Name, colour and category for this status."
              [icon]="detailsIcon">
              <div panelHeaderActions class="flex items-center gap-2">
                @if (status(); as current) {
                  <app-color-swatch variant="swatch" [color]="current.color" />

                  @if (current.isSystem) {
                    <app-badge shape="rounded" i18n="Marks a built-in item">
                      Built-in
                    </app-badge>
                  }
                }
              </div>
            </app-panel-header>

            @if (status(); as current) {
              <form class="grid gap-4 p-4" (submit)="save($event)">
                <app-form-input
                  [formField]="statusForm.name"
                  i18n-label="Label of the name field"
                  label="Name"
                  maxLength="128" />

                <app-form-textarea
                  [formField]="statusForm.description"
                  i18n-label="Label of the description field"
                  label="Description"
                  maxLength="512" />

                <app-color-select
                  [formField]="statusForm.color"
                  i18n-label="Label of the colour picker field"
                  label="Color" />

                <app-form-select
                  [formField]="statusForm.category"
                  i18n-label="Label of the status category field"
                  label="Category">
                  @for (category of categories; track category) {
                    <app-form-select-option [value]="category">
                      {{ categoryLabel(category) }}
                    </app-form-select-option>
                  }
                </app-form-select>

                @if (canManage()) {
                  <div
                    class="border-border flex flex-wrap items-center gap-3 border-t pt-4">
                    <button app-flat-button type="submit" [disabled]="saving()">
                      <span i18n="Button that saves changes to the status">
                        Save Status
                      </span>
                    </button>

                    <div class="ml-auto flex items-center gap-3">
                      @if (blockedReason(); as reason) {
                        <span class="text-muted text-sm">{{ reason }}</span>
                      }

                      <button
                        app-stroked-button
                        type="button"
                        [disabled]="!canDelete() || deleting()"
                        (click)="delete(current)">
                        <svg lucideTrash2 class="h-4 w-4"></svg>
                        <span i18n="Button that deletes a status">
                          Delete status
                        </span>
                      </button>
                    </div>
                  </div>
                }
              </form>
            }
          </app-panel>

          @if (hasReferences()) {
            <app-panel>
              <app-panel-header
                i18n-heading="Heading above the list of things using an item"
                heading="Used by"
                i18n-description="
                  Description of the list of things using an item
                "
                description="Everything else that points at this, besides tasks."
                [icon]="usageIcon" />

              <app-entity-usage-panel [usage]="usage.value()" />
            </app-panel>
          }

          <app-panel>
            <app-panel-header
              i18n-heading="Heading above the tasks using a status"
              heading="Tasks with this status"
              i18n-description="Explains that archived tasks are left out"
              description="Archived tasks are not counted."
              [icon]="tasksIcon">
              <div panelHeaderActions class="flex items-center gap-3">
                <app-badge color="info" shape="rounded">
                  <span
                    i18n="
                      Number of tasks using a status. COUNT is the number of
                      tasks
                    ">
                    {{
                      taskCount() // i18n(ph="COUNT")
                    }}
                    tasks
                  </span>
                </app-badge>

                <a
                  class="text-sm hover:underline"
                  [routerLink]="['/', workspaceId(), 'tasks']"
                  [queryParams]="taskListParams()">
                  <span
                    i18n="Link that opens the task list filtered to one item">
                    Open in task list
                  </span>
                </a>
              </div>
            </app-panel-header>

            <app-datatable
              [rounded]="false"
              i18n-errorMessage="Shown when a task list fails to load"
              errorMessage="Tasks could not be loaded."
              containerClass="max-h-[520px] overflow-auto border-0"
              tableClass="min-w-[720px] table-fixed"
              [data]="taskData"
              [stickyHeader]="true">
              <ng-template appDatatableCell="systemId" let-task>
                <app-task-scope-id [id]="task.systemId" />
              </ng-template>

              <ng-template appDatatableCell="name" let-task>
                <a
                  class="block truncate font-medium hover:underline"
                  [routerLink]="['/', workspaceId(), 'tasks', task.systemId]">
                  {{ task.name }}
                </a>
              </ng-template>

              <ng-template appDatatableCell="assignees" let-task>
                <div class="flex items-center gap-1">
                  @for (assignee of task.assignees; track assignee.id) {
                    <app-avatar
                      size="sm"
                      [name]="assignee.displayName"
                      [imageUrl]="assignee.pictureUrl"
                      [isServiceAccount]="assignee.isServiceAccount ?? false" />
                  }
                </div>
              </ng-template>

              <app-empty-state
                appDatatableEmpty
                compact
                i18n-title="Heading of an empty task list on a usage page"
                title="No tasks use this yet."
                i18n-description="
                  Explains that nothing currently uses the item being viewed
                "
                description="Nothing references it, so it is safe to change or remove.">
                <svg emptyStateIcon size="38" lucideListChecks></svg>
              </app-empty-state>
            </app-datatable>
          </app-panel>
        }
      </div>
    </app-page-container>
  `,
})
export class StatusDetailViewComponent {
  private readonly statusesService = inject(StatusesService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly id = input.required<string>();

  private readonly statusId = computed(() => {
    const parsed = Number(this.id());

    return Number.isInteger(parsed) ? parsed : null;
  });

  readonly statuses = statusResource();
  readonly usage = statusUsageResource(this.statusId);

  readonly workspaceId = inject(CurrentWorkspaceService).slug;
  readonly canManage = hasPermission(PERMISSONS.statuses.manage);

  readonly status = computed(() => {
    return this.statuses
      .value()
      .find((status) => status.id === this.statusId());
  });

  readonly listError = computed(() => !!this.statuses.error());
  readonly name = computed(() => this.usage.value()?.name ?? '');
  readonly taskCount = computed(() => this.usage.value()?.usageCount ?? 0);
  readonly blockedReason = computed(() => this.usage.value()?.blockedReason);
  readonly canDelete = computed(() => this.usage.value()?.canDelete ?? false);
  readonly deleting = signal(false);
  readonly saving = signal(false);
  readonly categories = statusCategoryOptions;
  readonly hasReferences = computed(() => {
    return (this.usage.value()?.references.length ?? 0) > 0;
  });

  readonly detailsIcon = LucideSettings2;
  readonly usageIcon = LucideShapes;
  readonly tasksIcon = LucideListChecks;

  readonly statusFormModel = signal({
    name: '',
    description: '',
    color: fallbackColor as string,
    category: StatusCategory.todo,
  });

  readonly statusForm = form(this.statusFormModel, (schema) => {
    apply(
      schema.name,
      requiredTextSchema({
        label: $localize`:Field name used inside validation messages, e.g. "Name is required.":Name`,
        maxLength: 128,
      })
    );
    maxLength(schema.description, 512);
    maxLength(schema.color, 32);
    required(schema.color);
    required(schema.category);
    disabled(schema, { when: () => !this.canManage() || this.saving() });
  });

  readonly taskListParams = computed(() => ({ statusIds: this.statusId() }));

  private readonly requestParams = computed(() => ({
    statusIds: this.statusId(),
  }));

  readonly taskData: DatatableDataSource<TaskViewModel> = {
    key: 'status-usage-tasks',
    columns: [
      {
        id: 'systemId',
        header: $localize`:Column heading for the task key:Key`,
        accessor: 'systemId',
        sortable: true,
        widthClass: 'w-28',
      },
      {
        id: 'name',
        header: $localize`:Column heading for the task name:Task`,
        accessor: 'name',
        sortable: true,
        cellClass: 'min-w-64',
      },
      {
        id: 'projectName',
        header: $localize`:Column heading for the project name:Project`,
        accessor: 'projectName',
        sortKey: 'projectName',
        widthClass: 'w-48',
      },
      {
        id: 'assignees',
        header: $localize`:Column heading for the task assignees:Assignees`,
        widthClass: 'w-40',
      },
    ],
    resource: {
      url: 'api/tasks',
      params: this.requestParams,
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, task: TaskViewModel) => task.id,
  };

  constructor() {
    effect(() => {
      const current = this.status();

      if (!current) return;

      this.statusFormModel.set({
        name: current.name,
        description: current.description ?? '',
        color: current.color ?? fallbackColor,
        category: current.category,
      });
    });
  }

  reload() {
    this.statuses.reload();
    this.usage.reload();
  }

  save(event: Event) {
    event.preventDefault();

    const current = this.status();

    if (!current) return;

    submit(this.statusForm, async () => {
      this.saving.set(true);

      const request = {
        id: current.id,
        entityType: current.entityType,
        name: this.statusForm.name().value().trim(),
        description: this.statusForm.description().value().trim() || null,
        color: this.statusForm.color().value(),
        category: this.statusForm.category().value(),
      };

      const update = this.statusesService
        .update(request)
        .pipe(finalize(() => this.saving.set(false)));

      const response = await firstValueFrom(update);

      if (!response.isSuccess) {
        return;
      }

      this.statuses.reload();
      this.usage.reload();
    });
  }

  categoryLabel(category: StatusCategory) {
    return statusCategoryLabels[category];
  }

  delete(status: Status) {
    this.confirmation
      .open({
        title: $localize`:Title of the confirmation dialog for deleting a status:Delete Status`,
        message: $localize`:Asks the user to confirm deleting a status. NAME is the status name:Delete "${status.name}:NAME:"? This cannot be undone.`,
        acceptLabel: $localize`:Confirms a destructive action:Delete`,
        cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
        color: 'warn',
      })
      .pipe(
        switchMap((confirmed) => {
          if (!confirmed) return EMPTY;

          this.deleting.set(true);

          return this.statusesService.delete(status.id);
        }),
        finalize(() => this.deleting.set(false))
      )
      .subscribe({
        next: (response) => {
          if (!response.isSuccess) {
            this.usage.reload();
            return;
          }

          void this.router.navigate(['..'], { relativeTo: this.route });
        },
      });
  }
}
