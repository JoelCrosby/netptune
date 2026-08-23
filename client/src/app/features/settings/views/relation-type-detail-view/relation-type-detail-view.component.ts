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
import { PERMISSIONS } from '@core/auth/permissions';
import { ConfirmationService } from '@core/services/confirmation.service';
import { relationTypeUsageResource } from '@core/resources/entity-usage.resource';
import { relationTypeResource } from '@core/resources/relation-type.resource';
import {
  RelationCategory,
  RelationType,
  isSymmetricCategory,
  relationCategoryLabels,
} from '@core/models/relation-type';
import { RelationTypeRelation } from '@core/models/task-relation';
import { RelationTypesService } from '@core/services/relation-types.service';
import { fallbackColor } from '@core/util/colors/colors';
import { requiredTextSchema } from '@core/util/forms/validation.schemas';
import {
  LucideLink,
  LucideShapes,
  LucideSpline,
  LucideTrash2,
} from '@lucide/angular';
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
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';
import { PanelComponent } from '@static/components/panel.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { EntityUsagePanelComponent } from '../../components/entity-usage-panel.component';
import { EMPTY, finalize, firstValueFrom, switchMap } from 'rxjs';

@Component({
  selector: 'app-relation-type-detail-view',
  imports: [
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
    FormTextAreaComponent,
    LucideLink,
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
        i18n-title="Page title for a single workspace relation type"
        title="Relation type" />

      <div class="flex flex-col gap-6">
        @if (usage.error() || listError()) {
          <app-error-state
            compact
            i18n-title="Shown when a relation type detail page fails to load"
            title="Relation type could not be loaded"
            i18n-description="Advice shown when a list fails to load"
            description="Check your connection and try again."
            (retry)="reload()" />
        } @else {
          <app-panel>
            <app-panel-header
              i18n-heading="Heading above the relation type edit form"
              heading="Relation details"
              i18n-description="Description of the relation type edit form"
              description="How this link reads in each direction."
              [icon]="detailsIcon">
              <div panelHeaderActions class="flex items-center gap-2">
                @if (relationType(); as current) {
                  <app-color-swatch variant="swatch" [color]="current.color" />

                  @if (current.isSystem) {
                    <app-badge shape="rounded" i18n="Marks a built-in item">
                      Built-in
                    </app-badge>
                  }
                }
              </div>
            </app-panel-header>

            @if (relationType(); as current) {
              <form class="grid gap-4 p-4" (submit)="save($event)">
                <p class="text-muted text-sm">
                  <span
                    i18n="
                      Explains that a relation type's category cannot be
                      changed. CATEGORY is the category name
                    ">
                    Category:
                    {{
                      categoryLabel(current.category)  // i18n(ph="CATEGORY")
                    }}. A relation type's category is fixed once it exists,
                    because changing it would hold existing links to rules they
                    were never checked against.
                  </span>
                </p>

                <app-form-input
                  [formField]="relationTypeForm.name"
                  i18n-label="Label of the name field"
                  label="Name"
                  maxLength="128" />

                @if (isSymmetric()) {
                  <p
                    class="text-muted text-sm"
                    i18n="
                      Shown when a relation reads the same in both directions
                    ">
                    Same both ways
                  </p>
                } @else {
                  <app-form-input
                    [formField]="relationTypeForm.inverseName"
                    i18n-label="
                      Label of the field for the reverse direction of a relation
                    "
                    label="Inverse name"
                    maxLength="128" />
                }

                <app-form-textarea
                  [formField]="relationTypeForm.description"
                  i18n-label="Label of the description field"
                  label="Description"
                  maxLength="512" />

                <app-color-select
                  [formField]="relationTypeForm.color"
                  i18n-label="Label of the colour picker field"
                  label="Color" />

                @if (canManage()) {
                  <div
                    class="border-border flex flex-wrap items-center gap-3 border-t pt-4">
                    <button app-flat-button type="submit" [disabled]="saving()">
                      <span
                        i18n="Button that saves changes to the relation type">
                        Save Relation Type
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
                        <span i18n="Button that deletes a relation type">
                          Delete relation type
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
              i18n-heading="Heading above the task links using a relation type"
              heading="Tasks linked by this relation"
              i18n-description="
                Explains that links to archived tasks are still listed
              "
              description="Links to archived tasks are kept and marked."
              [icon]="linksIcon">
              <app-badge panelHeaderActions color="info" shape="rounded">
                <span
                  i18n="
                    Number of task links using a relation type. COUNT is the
                    number of links
                  ">
                  {{
                    relationCount() // i18n(ph="COUNT")
                  }}
                  relations
                </span>
              </app-badge>
            </app-panel-header>

            <app-datatable
              [rounded]="false"
              i18n-errorMessage="Shown when the relation list fails to load"
              errorMessage="Relations could not be loaded."
              containerClass="max-h-[520px] overflow-auto border-0"
              tableClass="min-w-[760px] table-fixed"
              [data]="relationData()"
              [stickyHeader]="true">
              <ng-template appDatatableCell="sourceTask" let-relation>
                <div class="flex min-w-0 items-center gap-2">
                  <app-task-scope-id [id]="relation.sourceTask.systemId" />
                  <a
                    class="truncate hover:underline"
                    [routerLink]="[
                      '/',
                      workspaceId(),
                      'tasks',
                      relation.sourceTask.systemId,
                    ]">
                    {{ relation.sourceTask.name }}
                  </a>
                  @if (relation.sourceTask.isArchived) {
                    <app-badge shape="rounded" i18n="Marks a deleted task">
                      Archived
                    </app-badge>
                  }
                </div>
              </ng-template>

              <ng-template appDatatableCell="label">
                <span class="text-muted text-sm">{{ name() }}</span>
              </ng-template>

              <ng-template appDatatableCell="targetTask" let-relation>
                <div class="flex min-w-0 items-center gap-2">
                  <app-task-scope-id [id]="relation.targetTask.systemId" />
                  <a
                    class="truncate hover:underline"
                    [routerLink]="[
                      '/',
                      workspaceId(),
                      'tasks',
                      relation.targetTask.systemId,
                    ]">
                    {{ relation.targetTask.name }}
                  </a>
                  @if (relation.targetTask.isArchived) {
                    <app-badge shape="rounded" i18n="Marks a deleted task">
                      Archived
                    </app-badge>
                  }
                </div>
              </ng-template>

              <ng-template appDatatableEmpty>
                <app-empty-state
                  compact
                  i18n-title="Heading of an empty relation list on a usage page"
                  title="No tasks are linked by this relation yet."
                  i18n-description="
                    Explains that nothing currently uses the item being viewed
                  "
                  description="Nothing references it, so it is safe to change or remove.">
                  <svg emptyStateIcon size="38" lucideLink></svg>
                </app-empty-state>
              </ng-template>
            </app-datatable>
          </app-panel>
        }
      </div>
    </app-page-container>
  `,
})
export class RelationTypeDetailViewComponent {
  private readonly relationTypesService = inject(RelationTypesService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly id = input.required<string>();

  private readonly relationTypeId = computed(() => {
    const parsed = Number(this.id());

    return Number.isInteger(parsed) ? parsed : null;
  });

  readonly relationTypes = relationTypeResource();
  readonly usage = relationTypeUsageResource(this.relationTypeId);

  readonly workspaceId = inject(CurrentWorkspaceService).slug;
  readonly canManage = hasPermission(PERMISSIONS.relationTypes.manage);

  readonly relationType = computed(() => {
    return this.relationTypes
      .value()
      .find((relationType) => relationType.id === this.relationTypeId());
  });

  readonly listError = computed(() => !!this.relationTypes.error());
  readonly name = computed(() => this.usage.value()?.name ?? '');
  readonly relationCount = computed(() => this.usage.value()?.usageCount ?? 0);
  readonly blockedReason = computed(() => this.usage.value()?.blockedReason);
  readonly canDelete = computed(() => this.usage.value()?.canDelete ?? false);
  readonly deleting = signal(false);
  readonly saving = signal(false);
  readonly hasReferences = computed(() => {
    return (this.usage.value()?.references.length ?? 0) > 0;
  });

  readonly detailsIcon = LucideSpline;
  readonly usageIcon = LucideShapes;
  readonly linksIcon = LucideLink;

  readonly relationTypeFormModel = signal({
    name: '',
    inverseName: '',
    description: '',
    color: fallbackColor as string,
  });

  readonly relationTypeForm = form(this.relationTypeFormModel, (schema) => {
    apply(
      schema.name,
      requiredTextSchema({
        label: $localize`:Field name used inside validation messages, e.g. "Name is required.":Name`,
        maxLength: 128,
      })
    );
    maxLength(schema.inverseName, 128);
    maxLength(schema.description, 512);
    maxLength(schema.color, 32);
    required(schema.color);
    disabled(schema, () => !this.canManage() || this.saving());
  });

  readonly isSymmetric = computed(() => {
    const current = this.relationType();

    return current ? isSymmetricCategory(current.category) : false;
  });

  private readonly requestParams = computed(() => ({}));

  private readonly relationColumns = [
    {
      id: 'sourceTask',
      header: $localize`:Column heading for the source side of a task link:From`,
      cellClass: 'min-w-64',
    },
    {
      id: 'label',
      header: $localize`:Column heading for the name of a task link:Relation`,
      widthClass: 'w-40',
    },
    {
      id: 'targetTask',
      header: $localize`:Column heading for the target side of a task link:To`,
      cellClass: 'min-w-64',
    },
  ];

  readonly relationData = computed<DatatableDataSource<RelationTypeRelation>>(
    () => {
      return {
        key: 'relation-type-usage-relations',
        columns: this.relationColumns,
        resource: {
          url: `api/relation-types/${this.relationTypeId()}/relations`,
          params: this.requestParams,
        },
        rows: (response) => response?.payload?.items ?? [],
        trackBy: (_: number, relation: RelationTypeRelation) => relation.id,
      };
    }
  );

  constructor() {
    effect(() => {
      const current = this.relationType();

      if (!current) return;

      this.relationTypeFormModel.set({
        name: current.name,
        inverseName: current.inverseName,
        description: current.description ?? '',
        color: current.color ?? fallbackColor,
      });
    });
  }

  reload() {
    this.relationTypes.reload();
    this.usage.reload();
  }

  save(event: Event) {
    event.preventDefault();

    const current = this.relationType();

    if (!current) return;

    submit(this.relationTypeForm, async () => {
      this.saving.set(true);

      const name = this.relationTypeForm.name().value().trim();
      const inverseName = this.isSymmetric()
        ? name
        : this.relationTypeForm.inverseName().value().trim() || name;

      const request = {
        id: current.id,
        name,
        inverseName,
        description: this.relationTypeForm.description().value().trim() || null,
        color: this.relationTypeForm.color().value(),
      };

      const update = this.relationTypesService
        .update(request)
        .pipe(finalize(() => this.saving.set(false)));

      const response = await firstValueFrom(update);

      if (!response.isSuccess) return;

      this.relationTypes.reload();
      this.usage.reload();
    });
  }

  categoryLabel(category: RelationCategory) {
    return relationCategoryLabels[category];
  }

  delete(relationType: RelationType) {
    this.confirmation
      .open({
        title: $localize`:Title of the confirmation dialog for deleting a relation type:Delete Relation Type`,
        message: $localize`:Asks the user to confirm deleting a relation type. NAME is the relation type name:Delete "${relationType.name}:NAME:"? This cannot be undone.`,
        acceptLabel: $localize`:Confirms a destructive action:Delete`,
        cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
        color: 'warn',
      })
      .pipe(
        switchMap((confirmed) => {
          if (!confirmed) return EMPTY;

          this.deleting.set(true);

          return this.relationTypesService.delete(relationType.id);
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
