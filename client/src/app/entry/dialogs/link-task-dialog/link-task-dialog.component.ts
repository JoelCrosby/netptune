import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { httpResource } from '@angular/common/http';
import {
  Component,
  computed,
  inject,
  linkedSignal,
  signal,
} from '@angular/core';
import {
  RelationType,
  isSymmetricCategory,
  relationCategoryDescriptions,
} from '@core/models/relation-type';
import { TaskRelation } from '@core/models/task-relation';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { relationTypeResource } from '@core/resources/relation-type.resource';
import { LucideLink2, LucideX } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { ColorSwatchComponent } from '@static/components/color-swatch/color-swatch.component';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { LinkTaskListComponent } from './link-task-list.component';

export interface LinkTaskDialogData {
  // Absent while linking from the create-task dialog, where the task does not exist yet.
  task?: TaskViewModel;
}

export interface LinkTaskDialogResult {
  relationTypeId: number;
  relationType: RelationType;
  isForward: boolean;
  tasks: readonly TaskViewModel[];
}

interface RelationOption {
  key: string;
  relationType: RelationType;
  isForward: boolean;
  verb: string;
  description: string;
}

@Component({
  selector: 'app-link-task-dialog',
  imports: [
    ColorSwatchComponent,
    DialogCloseDirective,
    FlatButtonComponent,
    IconButtonComponent,
    LinkTaskListComponent,
    LucideLink2,
    LucideX,
    StrokedButtonComponent,
    TaskScopeIdComponent,
  ],
  host: { class: 'flex min-h-0 flex-auto flex-col' },
  template: `
    <div class="border-border relative flex-none border-b px-6 py-5">
      <h1 class="m-0 pr-10 text-xl font-medium">{{ title() }}</h1>
      <p class="text-muted mt-1 text-sm">{{ subtitle() }}</p>

      <button
        class="absolute top-4 right-4"
        app-icon-button
        app-dialog-close
        type="button"
        i18n-aria-label="Accessible label for the button that closes a dialog"
        aria-label="Close dialog">
        <svg lucideX class="h-5 w-5" aria-hidden="true"></svg>
      </button>
    </div>

    <div
      class="custom-scroll min-h-0 flex-auto overflow-y-auto lg:grid lg:grid-cols-[minmax(0,1fr)_22rem] lg:overflow-hidden">
      <app-link-task-list
        class="border-border p-5 lg:min-h-80 lg:border-r"
        [excludeTaskId]="task?.id"
        [selectedIds]="selectedIds()"
        [linkedIds]="duplicateIds()"
        (toggled)="toggle($event)" />

      <aside
        class="bg-secondary-background custom-scroll flex flex-col gap-6 p-5 lg:min-h-0 lg:overflow-y-auto">
        <section class="flex flex-col gap-2">
          <h2
            class="text-muted text-xs font-semibold tracking-wide uppercase"
            i18n="
              Heading over the choices for how two tasks relate to each other
            ">
            Relationship
          </h2>

          <div class="flex flex-wrap gap-1.5">
            @for (option of relationOptions(); track option.key) {
              <button
                type="button"
                class="focus-visible:ring-primary flex cursor-pointer items-center gap-2 rounded-full border px-3 py-1.5 text-[13px] font-medium whitespace-nowrap transition-colors focus-visible:ring-2 focus-visible:outline-none"
                [class]="pillClass(option)"
                [attr.aria-pressed]="option.key === selectedKey()"
                (click)="selectedKey.set(option.key)">
                <app-color-swatch
                  size="sm"
                  [color]="option.relationType.color" />
                {{ option.verb }}
              </button>
            } @empty {
              <p
                class="text-muted text-[13px] leading-relaxed"
                i18n="
                  Shown in the link dialog when the workspace has no relation
                  types to pick from
                ">
                This workspace has no relation types yet. An administrator can
                add them in workspace settings.
              </p>
            }
          </div>

          <p class="text-muted text-[13px] leading-relaxed">
            {{ selectedOption()?.description }}
          </p>
        </section>

        <section class="flex flex-col gap-2">
          <h2 class="text-muted text-xs font-semibold tracking-wide uppercase">
            {{ pendingLabel() }}
          </h2>

          @for (pending of selected(); track pending.id) {
            <div
              class="border-primary/25 bg-primary/8 flex items-center gap-2 rounded-md border px-2.5 py-2">
              <app-task-scope-id class="shrink-0" [id]="pending.systemId" />

              <span class="min-w-0 flex-1 truncate text-[13px]">
                {{ pending.name }}
              </span>

              <button
                app-icon-button
                class="h-6 w-6 shrink-0"
                type="button"
                i18n-aria-label="
                  Accessible label for the button that drops a task from the
                  list waiting to be linked
                "
                aria-label="Remove from list"
                (click)="toggle(pending)">
                <svg lucideX class="h-3.5 w-3.5" aria-hidden="true"></svg>
              </button>
            </div>
          } @empty {
            <div
              class="border-border text-muted flex items-center gap-2 rounded-md border border-dashed px-3 py-3.5 text-[13px]">
              <svg
                lucideLink2
                class="h-4 w-4 shrink-0"
                aria-hidden="true"></svg>
              <span
                i18n="Empty state of the list of tasks queued up to be linked">
                Pick tasks on the left to build the list.
              </span>
            </div>
          }
        </section>

        @if (existingRelations().length) {
          <section class="flex flex-col gap-2">
            <h2
              class="text-muted text-xs font-semibold tracking-wide uppercase"
              i18n="Heading over the links a task already has">
              Already linked
            </h2>

            @for (relation of existingRelations(); track relation.id) {
              <div
                class="border-border bg-background flex items-center gap-2 rounded-md border px-2.5 py-2">
                <app-color-swatch
                  size="sm"
                  [color]="relation.relationTypeColor" />

                <span class="text-muted shrink-0 text-xs whitespace-nowrap">
                  {{ relation.label }}
                </span>

                <app-task-scope-id
                  class="shrink-0"
                  [id]="relation.relatedTask.systemId" />

                <span class="text-muted min-w-0 flex-1 truncate text-[13px]">
                  {{ relation.relatedTask.name }}
                </span>
              </div>
            }
          </section>
        }
      </aside>
    </div>

    <div
      class="border-border flex flex-none flex-wrap items-center justify-between gap-3 border-t px-6 py-4">
      <p class="text-muted text-sm">{{ summary() }}</p>

      <div class="flex items-center gap-2">
        <button app-stroked-button type="button" (click)="close()">
          <span i18n="Dismisses a dialog without acting">Cancel</span>
        </button>
        <button
          app-flat-button
          color="primary"
          type="button"
          [disabled]="selected().length === 0 || !selectedOption()"
          (click)="submit()">
          {{ ctaLabel() }}
        </button>
      </div>
    </div>
  `,
})
export class LinkTaskDialogComponent {
  static readonly panelClass = 'app-link-task-dialog';

  private readonly dialogRef =
    inject<DialogRef<LinkTaskDialogResult, LinkTaskDialogComponent>>(DialogRef);
  private readonly dialogData = inject<LinkTaskDialogData>(DIALOG_DATA);

  protected readonly task = this.dialogData.task;

  private readonly relationTypesResource = relationTypeResource();

  private readonly relationTypes = computed(() =>
    [...this.relationTypesResource.value()].sort(
      (a, b) => a.sortOrder - b.sortOrder || a.id - b.id
    )
  );

  // One pill per readable direction, so picking a relation and picking which way
  // it points is a single choice rather than two selects to compose.
  protected readonly relationOptions = computed<RelationOption[]>(() => {
    const options: RelationOption[] = [];

    for (const relationType of this.relationTypes()) {
      const description = relationCategoryDescriptions[relationType.category];

      options.push({
        key: `${relationType.id}:forward`,
        relationType,
        isForward: true,
        verb: relationType.name,
        description,
      });

      const hasInverse =
        !isSymmetricCategory(relationType.category) &&
        !!relationType.inverseName;

      if (!hasInverse) continue;

      options.push({
        key: `${relationType.id}:inverse`,
        relationType,
        isForward: false,
        verb: relationType.inverseName,
        description,
      });
    }

    return options;
  });

  // The relation types arrive after the dialog opens, so the first pill is only
  // chosen once there is one to choose.
  protected readonly selectedKey = linkedSignal<
    readonly RelationOption[],
    string | null
  >({
    source: this.relationOptions,
    computation: (options, previous) => {
      const previousKey = previous?.value ?? null;
      const stillOffered = options.some((option) => option.key === previousKey);

      if (stillOffered) return previousKey;

      return options[0]?.key ?? null;
    },
  });

  protected readonly selectedOption = computed(() => {
    const key = this.selectedKey();

    return this.relationOptions().find((option) => option.key === key);
  });

  protected readonly selected = signal<readonly TaskViewModel[]>([]);

  protected readonly selectedIds = computed(
    () => new Set(this.selected().map((task) => task.id))
  );

  private readonly relations = httpResource<TaskRelation[]>(
    () => {
      const systemId = this.task?.systemId;

      if (!systemId) return undefined;

      return { url: `api/task-relations/${systemId}` };
    },
    { defaultValue: [] }
  );

  protected readonly existingRelations = this.relations.value;

  // A link is a duplicate only when the relation type and the direction both
  // match, so the picked tasks stay selectable under a different relationship.
  protected readonly duplicateIds = computed(() => {
    const option = this.selectedOption();
    const ids = new Set<number>();

    if (!option) return ids;

    const symmetric = isSymmetricCategory(option.relationType.category);

    for (const relation of this.existingRelations()) {
      if (relation.relationTypeId !== option.relationType.id) continue;
      if (!symmetric && relation.isSource !== option.isForward) continue;

      ids.add(relation.relatedTask.id);
    }

    return ids;
  });

  protected readonly title = computed(() => {
    const systemId = this.task?.systemId;

    if (!systemId) {
      return $localize`:Title of the dialog for linking tasks together:Link tasks`;
    }

    return $localize`:Title of the dialog for linking tasks to the task named:Link tasks to ${systemId}:SYSTEM_ID:`;
  });

  protected readonly subtitle = computed(() => {
    return (
      this.task?.name ??
      $localize`:Explains that links picked here are saved with the new task:The links you pick are created with the task.`
    );
  });

  protected readonly pendingLabel = computed(() => {
    const count = this.selected().length;

    if (count === 0) {
      return $localize`:Heading over the list of links waiting to be created:Links to create`;
    }

    if (count === 1) {
      return $localize`:Heading over a single link waiting to be created:Will create 1 link`;
    }

    return $localize`:Heading over the links waiting to be created:Will create ${count}:COUNT: links`;
  });

  protected readonly summary = computed(() => {
    const option = this.selectedOption();

    if (!option) return '';

    const count = this.selected().length;

    if (count === 0) {
      return $localize`:Prompt shown while nothing is selected in the link dialog:Pick at least one task to link.`;
    }

    const subject =
      this.task?.systemId ??
      $localize`:Stands in for a task that has not been created yet:This task`;

    if (count === 1) {
      return $localize`:Describes the single link that will be created:${subject}:SUBJECT: ${option.verb}:VERB: 1 task.`;
    }

    return $localize`:Describes the links that will be created:${subject}:SUBJECT: ${option.verb}:VERB: ${count}:COUNT: tasks.`;
  });

  protected readonly ctaLabel = computed(() => {
    const count = this.selected().length;

    if (count === 0) {
      return $localize`:Button that creates the task links:Link tasks`;
    }

    if (count === 1) {
      return $localize`:Button that creates one task link:Link 1 task`;
    }

    return $localize`:Button that creates several task links:Link ${count}:COUNT: tasks`;
  });

  protected pillClass(option: RelationOption) {
    if (option.key === this.selectedKey()) {
      return 'border-primary bg-primary/12 text-foreground';
    }

    return 'border-border bg-background text-muted hover:border-primary/60 hover:text-foreground';
  }

  protected toggle(task: TaskViewModel) {
    this.selected.update((selected) => {
      const alreadyPicked = selected.some((picked) => picked.id === task.id);

      if (alreadyPicked) {
        return selected.filter((picked) => picked.id !== task.id);
      }

      return [...selected, task];
    });
  }

  protected submit() {
    const option = this.selectedOption();
    const tasks = this.selected();

    if (!option || tasks.length === 0) return;

    this.dialogRef.close({
      relationTypeId: option.relationType.id,
      relationType: option.relationType,
      isForward: option.isForward,
      tasks,
    });
  }

  protected close() {
    this.dialogRef.close();
  }
}
