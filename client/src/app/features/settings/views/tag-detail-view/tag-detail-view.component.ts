import {
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';
import {
  apply,
  disabled,
  FormField,
  form,
  submit,
} from '@angular/forms/signals';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { netptunePermissions } from '@core/auth/permissions';
import { ConfirmationService } from '@core/services/confirmation.service';
import { tagUsageResource } from '@core/resources/entity-usage.resource';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { selectHasPermission } from '@core/store/auth/auth.selectors';
import * as actions from '@core/store/tags/tags.actions';
import { TagsService } from '@core/store/tags/tags.service';
import { selectCurrentWorkspaceIdentifier } from '@core/store/workspaces/workspaces.selectors';
import { requiredTextSchema } from '@core/util/forms/validation.schemas';
import {
  LucideListChecks,
  LucideShapes,
  LucideTag,
  LucideTrash2,
} from '@lucide/angular';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableEmptyDirective } from '@static/components/datatable/datatable-empty.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import { DatatableDataSource } from '@static/components/datatable/datatable.types';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { PanelComponent } from '@static/components/panel.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { EntityUsagePanelComponent } from '../../components/entity-usage-panel.component';
import { EMPTY, finalize, firstValueFrom, switchMap } from 'rxjs';

@Component({
  selector: 'app-tag-detail-view',
  imports: [
    AvatarComponent,
    BadgeComponent,
    DatatableCellTemplateDirective,
    DatatableComponent,
    DatatableEmptyDirective,
    EmptyStateComponent,
    EntityUsagePanelComponent,
    ErrorStateComponent,
    FlatButtonComponent,
    FormField,
    FormInputComponent,
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
        i18n-title="Page title for a single workspace tag"
        title="Tag" />

      <div class="flex flex-col gap-6">
        @if (usage.error()) {
          <app-error-state
            compact
            i18n-title="Shown when a tag detail page fails to load"
            title="Tag could not be loaded"
            i18n-description="Advice shown when a list fails to load"
            description="Check your connection and try again."
            (retry)="usage.reload()" />
        } @else {
          <app-panel>
            <app-panel-header
              i18n-heading="Heading above the tag edit form"
              heading="Tag details"
              i18n-description="Description of the tag edit form"
              description="Renaming a tag renames it on every task that carries it."
              [icon]="detailsIcon" />

            <form class="grid gap-4 p-4" (submit)="save($event)">
              <app-form-input
                [formField]="tagForm.name"
                i18n-label="Label of the name field"
                label="Name"
                maxLength="128" />

              @if (canUpdate() || canDelete()) {
                <div
                  class="border-border flex flex-wrap items-center gap-3 border-t pt-4">
                  @if (canUpdate()) {
                    <button app-flat-button type="submit" [disabled]="saving()">
                      <span i18n="Button that saves changes to the tag">
                        Save Tag
                      </span>
                    </button>
                  }

                  @if (canDelete()) {
                    <button
                      app-stroked-button
                      class="ml-auto"
                      type="button"
                      [disabled]="deleting()"
                      (click)="delete()">
                      <svg lucideTrash2 class="h-4 w-4"></svg>
                      <span i18n="Button that deletes a tag">Delete tag</span>
                    </button>
                  }
                </div>
              }
            </form>
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
              i18n-heading="Heading above the tasks using a tag"
              heading="Tasks with this tag"
              i18n-description="Explains that archived tasks are left out"
              description="Archived tasks are not counted."
              [icon]="tasksIcon">
              <div panelHeaderActions class="flex items-center gap-3">
                <app-badge color="info" shape="rounded">
                  <span
                    i18n="
                      Number of tasks using a tag. COUNT is the number of tasks
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
export class TagDetailViewComponent {
  private readonly store = inject(Store);
  private readonly tagsService = inject(TagsService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly id = input.required<string>();

  private readonly tagId = computed(() => {
    const parsed = Number(this.id());

    return Number.isInteger(parsed) ? parsed : null;
  });

  readonly usage = tagUsageResource(this.tagId);

  readonly workspaceId = this.store.selectSignal(
    selectCurrentWorkspaceIdentifier
  );
  readonly canUpdate = this.store.selectSignal(
    selectHasPermission(netptunePermissions.tags.update)
  );
  readonly canDelete = this.store.selectSignal(
    selectHasPermission(netptunePermissions.tags.delete)
  );

  readonly deleting = signal(false);
  readonly saving = signal(false);
  readonly hasReferences = computed(() => {
    return (this.usage.value()?.references.length ?? 0) > 0;
  });

  readonly detailsIcon = LucideTag;
  readonly usageIcon = LucideShapes;
  readonly tasksIcon = LucideListChecks;

  readonly tagFormModel = signal({ name: '' });

  readonly tagForm = form(this.tagFormModel, (schema) => {
    apply(
      schema.name,
      requiredTextSchema({
        label: $localize`:Field name used inside validation messages, e.g. "Name is required.":Name`,
        maxLength: 128,
        minLength: 2,
      })
    );
    disabled(schema, () => !this.canUpdate() || this.saving());
  });

  readonly name = computed(() => this.usage.value()?.name ?? '');
  readonly taskCount = computed(() => this.usage.value()?.usageCount ?? 0);
  readonly taskListParams = computed(() => ({ tags: this.name() }));

  private readonly requestParams = computed(() => ({ tags: this.name() }));

  readonly taskData: DatatableDataSource<TaskViewModel> = {
    key: 'tag-usage-tasks',
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
      const current = this.usage.value();

      if (!current) return;

      this.tagFormModel.set({ name: current.name });
    });
  }

  save(event: Event) {
    event.preventDefault();

    submit(this.tagForm, async () => {
      const currentValue = this.name();
      const newValue = this.tagForm.name().value().trim();
      const isUnchanged = !currentValue || newValue === currentValue;

      if (isUnchanged) return;

      this.saving.set(true);

      const rename = this.tagsService
        .patch({ currentValue, newValue })
        .pipe(finalize(() => this.saving.set(false)));

      const response = await firstValueFrom(rename);

      if (!response.isSuccess) return;

      this.store.dispatch(actions.loadTags.init());
      this.usage.reload();
    });
  }

  delete() {
    const name = this.name();
    if (!name) return;

    this.confirmation
      .open({
        title: $localize`:Title of the confirmation dialog for deleting a tag:Delete Tag`,
        message: $localize`:Asks the user to confirm deleting a tag. NAME is the tag name, COUNT is how many tasks carry it:Delete "${name}:NAME:"? It will be removed from ${this.taskCount()}:COUNT: tasks.`,
        acceptLabel: $localize`:Confirms a destructive action:Delete`,
        cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
        color: 'warn',
      })
      .pipe(
        switchMap((confirmed) => {
          if (!confirmed) return EMPTY;

          this.deleting.set(true);

          return this.tagsService.delete({ tags: [name] });
        }),
        finalize(() => this.deleting.set(false))
      )
      .subscribe({
        next: (response) => {
          if (!response.isSuccess) {
            this.usage.reload();
            return;
          }

          this.store.dispatch(actions.loadTags.init());
          void this.router.navigate(['..'], { relativeTo: this.route });
        },
      });
  }
}
