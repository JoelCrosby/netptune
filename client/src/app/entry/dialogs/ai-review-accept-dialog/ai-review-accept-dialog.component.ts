import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject, signal } from '@angular/core';
import { TaskPriority, taskPriorityLabels } from '@core/enums/task-priority';
import {
  AiReviewFinding,
  AiReviewSplitTask,
  AiReviewSuggestion,
  DecideAiReviewFindingRequest,
} from '@core/models/ai-review';
import { RelationCategory } from '@core/models/relation-type';
import { applyReviewEdit } from '@core/util/ai-review-edit';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CalloutComponent } from '@static/components/callout/callout.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { DialogContentComponent } from '@static/components/dialog-content/dialog-content.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { FormControlFieldComponent } from '@static/components/form-control/form-control-field.component';
import { FormControlInputDirective } from '@static/components/form-control/form-control.directives';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';
import { PanelComponent } from '@static/components/panel.component';
import {
  SegmentedControlComponent,
  SegmentedOption,
} from '@static/components/segmented-control/segmented-control.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { DiffMode } from '@static/components/text-diff/text-diff';
import { TextDiffComponent } from '@static/components/text-diff/text-diff.component';
import { ValueChipComponent } from '@static/components/value-chip/value-chip.component';
import { ValueSwapComponent } from '@static/components/value-swap/value-swap.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';

export interface AiReviewAcceptTaskState {
  tags: string[];
  estimateValue?: number | null;
  priority?: TaskPriority | null;
}

export interface AiReviewAcceptDialogData {
  finding: AiReviewFinding;
  suggestion: AiReviewSuggestion;
  currentText: string;
  task: AiReviewAcceptTaskState;
  relatedTaskName?: string | null;
  canCreateTags: boolean;
}

export type AiReviewAcceptDialogResult = Omit<
  DecideAiReviewFindingRequest,
  'decision'
>;

interface SplitRow {
  name: string;
  description: string | null;
  kept: boolean;
}

interface TagRow {
  name: string;
  isNew: boolean;
  kept: boolean;
}

type PreviewMode = Extract<DiffMode, 'split' | 'unified'>;

const maxSplitTaskNameLength = 200;
const maxTagNameLength = 128;

@Component({
  selector: 'app-ai-review-accept-dialog',
  imports: [
    BadgeComponent,
    CalloutComponent,
    CheckboxComponent,
    DialogActionsDirective,
    DialogCloseDirective,
    DialogContentComponent,
    DialogTitleComponent,
    FlatButtonComponent,
    FormControlFieldComponent,
    FormControlInputDirective,
    FormTextAreaComponent,
    PanelComponent,
    SegmentedControlComponent,
    StrokedButtonComponent,
    TaskScopeIdComponent,
    TextDiffComponent,
    ValueChipComponent,
    ValueSwapComponent,
  ],
  template: `
    <app-dialog-title
      i18n="Title of the dialog that previews a review suggestion">
      Accept suggestion
    </app-dialog-title>

    <app-dialog-content>
      <div class="flex flex-col gap-4">
        <div>
          <p class="font-semibold">{{ data.finding.title }}</p>
          <p class="text-muted mt-1 text-sm">{{ data.finding.explanation }}</p>
        </div>

        @switch (data.suggestion.kind) {
          @case ('edit') {
            <app-form-textarea
              i18n-label="
                Label above the editable text a review suggestion will write
              "
              label="Suggested text"
              rows="5"
              [noMargin]="true"
              [(value)]="replacement" />

            @if (after() === null) {
              <app-callout
                color="warn"
                i18n-title="
                  Title of the notice shown when a review suggestion no longer
                  fits the description
                "
                title="The description changed since this review">
                <span
                  i18n="
                    Explains why a review suggestion can no longer be applied
                  ">
                  The passage this suggestion replaces is no longer in the
                  description. Edit the description by hand or run the review
                  again.
                </span>
              </app-callout>
            } @else {
              <div class="flex items-center justify-between gap-2">
                <span
                  class="text-sm font-medium"
                  i18n="
                    Heading above the before and after preview of a review
                    suggestion
                  ">
                  Preview
                </span>
                <app-segmented-control
                  i18n-ariaLabel="Accessible label of the diff layout switch"
                  ariaLabel="Preview layout"
                  [options]="modes"
                  [(value)]="mode" />
              </div>

              <app-panel class="max-h-[50vh] overflow-auto">
                <app-text-diff
                  [before]="data.currentText"
                  [after]="after()"
                  [mode]="mode()" />
              </app-panel>
            }
          }
          @case ('split') {
            <p
              class="text-sm"
              i18n="Explains what accepting a suggested split of a task does">
              Each task you keep is created in the same project and linked as a
              child of this one.
            </p>
            <ul class="flex flex-col gap-2">
              @for (row of splitRows(); track $index) {
                <li class="flex items-start gap-2">
                  <app-checkbox
                    class="mt-2.5"
                    density="compact"
                    [checked]="row.kept"
                    (changed)="setSplitKept($index, $event)">
                    <span class="sr-only">{{ keepLabel }}</span>
                  </app-checkbox>
                  <div class="flex min-w-0 flex-1 flex-col gap-1">
                    <app-form-control-field density="compact">
                      <input
                        appFormInput
                        class="px-3"
                        [value]="row.name"
                        [attr.maxlength]="maxSplitTaskNameLength"
                        [attr.aria-label]="splitNameLabel"
                        (input)="
                          renameSplit($index, $any($event.target).value)
                        " />
                    </app-form-control-field>
                    @if (row.description) {
                      <p class="text-muted text-xs">{{ row.description }}</p>
                    }
                  </div>
                </li>
              }
            </ul>
          }
          @case ('link') {
            <div class="flex items-center gap-2 text-sm">
              <span>{{ linkLabel() }}</span>
              <app-task-scope-id [id]="linkSystemId()" />
              @if (data.relatedTaskName; as name) {
                <span class="truncate font-medium">{{ name }}</span>
              }
            </div>
          }
          @case ('metadata') {
            @if (!hasMetadataToOffer()) {
              <p
                class="text-muted text-sm"
                i18n="
                  Shown when everything a review suggests adding is already on
                  the task
                ">
                Everything this suggests is already on the task.
              </p>
            }

            @if (tagRows().length) {
              <div class="flex flex-col gap-2">
                <span
                  class="text-sm font-medium"
                  i18n="Heading above the tags a review suggests adding">
                  Tags to add
                </span>
                @for (row of tagRows(); track $index) {
                  <div class="flex items-center gap-2">
                    @if (row.isNew) {
                      <app-checkbox
                        density="compact"
                        [checked]="row.kept"
                        [disabled]="!data.canCreateTags"
                        (changed)="setTagKept($index, $event)">
                        <span class="sr-only">{{ keepLabel }}</span>
                      </app-checkbox>
                      <app-form-control-field
                        density="compact"
                        class="min-w-0 flex-1"
                        [disabled]="!data.canCreateTags">
                        <input
                          appFormInput
                          class="px-3"
                          [value]="row.name"
                          [disabled]="!data.canCreateTags"
                          [attr.maxlength]="maxTagNameLength"
                          [attr.aria-label]="tagNameLabel"
                          (input)="
                            renameTag($index, $any($event.target).value)
                          " />
                      </app-form-control-field>
                      <app-badge
                        color="primary"
                        i18n="
                          Marks a tag a review suggests that does not exist yet
                        ">
                        New tag
                      </app-badge>
                    } @else {
                      <app-checkbox
                        density="compact"
                        [checked]="row.kept"
                        (changed)="setTagKept($index, $event)">
                        {{ row.name }}
                      </app-checkbox>
                    }
                  </div>
                }
                @if (hasNewTags() && !data.canCreateTags) {
                  <p
                    class="text-muted text-xs"
                    i18n="Explains why new tags from a review cannot be chosen">
                    You don't have permission to create tags, so only existing
                    tags can be added.
                  </p>
                }
              </div>
            }

            @if (suggestedEstimateLabel(); as estimate) {
              <app-checkbox
                density="compact"
                [checked]="applyEstimate()"
                (changed)="applyEstimate.set($event)">
                <span class="flex flex-wrap items-center gap-2">
                  <span i18n="Label of the estimate change a review suggests"
                    >Estimate</span
                  >
                  <app-value-swap
                    density="inline"
                    [hasBefore]="!!currentEstimate()">
                    @if (currentEstimate(); as current) {
                      <app-value-chip
                        before
                        tone="removed"
                        mono
                        [label]="current" />
                    }
                    <app-value-chip
                      after
                      tone="added"
                      mono
                      [label]="estimate" />
                  </app-value-swap>
                </span>
              </app-checkbox>
            }

            @if (suggestedPriority(); as priority) {
              <app-checkbox
                density="compact"
                [checked]="applyPriority()"
                (changed)="applyPriority.set($event)">
                <span class="flex flex-wrap items-center gap-2">
                  <span i18n="Label of the priority change a review suggests"
                    >Priority</span
                  >
                  <app-value-swap
                    density="inline"
                    [hasBefore]="!!currentPriority()">
                    @if (currentPriority(); as current) {
                      <app-value-chip before tone="removed" [label]="current" />
                    }
                    <app-value-chip after tone="added" [label]="priority" />
                  </app-value-swap>
                </span>
              </app-checkbox>
            }
          }
        }
      </div>
    </app-dialog-content>

    <div app-dialog-actions align="end">
      <button app-stroked-button app-dialog-close type="button">
        <span i18n="Dismisses a dialog without saving">Close</span>
      </button>
      <button
        app-flat-button
        type="button"
        [disabled]="!canConfirm()"
        (click)="confirm()">
        {{ confirmLabel() }}
      </button>
    </div>
  `,
})
export class AiReviewAcceptDialogComponent {
  private readonly dialogRef =
    inject<
      DialogRef<AiReviewAcceptDialogResult, AiReviewAcceptDialogComponent>
    >(DialogRef);

  readonly data = inject<AiReviewAcceptDialogData>(DIALOG_DATA);

  readonly maxSplitTaskNameLength = maxSplitTaskNameLength;
  readonly maxTagNameLength = maxTagNameLength;

  readonly keepLabel = $localize`:Accessible label of the checkbox that keeps an item a review suggests:Keep`;
  readonly splitNameLabel = $localize`:Accessible label of the name of a task a review suggests creating:Task name`;
  readonly tagNameLabel = $localize`:Accessible label of the name of a tag a review suggests creating:Tag name`;

  readonly mode = signal<PreviewMode>('split');
  readonly modes: SegmentedOption<PreviewMode>[] = [
    {
      value: 'split',
      label: $localize`:Diff layout showing before and after side by side:Side by side`,
    },
    {
      value: 'unified',
      label: $localize`:Diff layout showing changes in one column:Inline`,
    },
  ];

  private readonly edit = computed(() => {
    const suggestion = this.data.suggestion;

    return suggestion.kind === 'edit' ? suggestion : null;
  });

  readonly metadata = computed(() => {
    const suggestion = this.data.suggestion;

    return suggestion.kind === 'metadata' ? suggestion : null;
  });

  readonly replacement = signal(this.edit()?.replace ?? '');

  readonly after = computed(() => {
    const edit = this.edit();

    if (!edit) return null;

    return applyReviewEdit(
      this.data.currentText,
      edit.find,
      this.replacement()
    );
  });

  readonly splitRows = signal<SplitRow[]>(this.initialSplitRows());
  readonly tagRows = signal<TagRow[]>(this.initialTagRows());
  readonly applyEstimate = signal(true);
  readonly applyPriority = signal(true);

  readonly hasNewTags = computed(() => {
    return this.tagRows().some((row) => row.isNew);
  });

  readonly hasMetadataToOffer = computed(() => {
    const hasEstimate = this.suggestedEstimate() !== null;
    const hasPriority = this.suggestedPriority() !== null;

    return this.tagRows().length > 0 || hasEstimate || hasPriority;
  });

  readonly suggestedEstimate = computed(() => {
    return this.metadata()?.estimateValue ?? null;
  });

  readonly suggestedEstimateLabel = computed(() => {
    const estimate = this.suggestedEstimate();

    return estimate === null ? null : `${estimate}`;
  });

  readonly currentEstimate = computed(() => {
    const estimate = this.data.task.estimateValue;
    const hasEstimate = estimate !== null && estimate !== undefined;

    return hasEstimate ? `${estimate}` : null;
  });

  readonly suggestedPriority = computed(() => {
    const priority = this.metadata()?.priority;

    return priority ? taskPriorityLabels[priority] : null;
  });

  readonly currentPriority = computed(() => {
    const priority = this.data.task.priority;

    return priority ? taskPriorityLabels[priority] : null;
  });

  readonly linkSystemId = computed(() => {
    const suggestion = this.data.suggestion;

    return suggestion.kind === 'link' ? suggestion.systemId : '';
  });

  readonly linkLabel = computed(() => {
    const suggestion = this.data.suggestion;
    const isDuplicate =
      suggestion.kind === 'link' &&
      suggestion.category === RelationCategory.duplicate;

    return isDuplicate
      ? $localize`:Describes a suggested link that marks this task as a duplicate of another:Mark this task as a duplicate of`
      : $localize`:Describes a suggested link that relates this task to another:Link this task as related to`;
  });

  readonly canConfirm = computed(() => {
    switch (this.data.suggestion.kind) {
      case 'edit':
        return this.after() !== null;
      case 'split':
        return this.keptSplitTasks().length > 0;
      case 'metadata':
        return this.hasMetadataChoice();
      default:
        return true;
    }
  });

  readonly confirmLabel = computed(() => {
    switch (this.data.suggestion.kind) {
      case 'split':
        return $localize`:Button that creates the tasks a review suggests. COUNT is how many:Create ${this.keptSplitTasks().length}:COUNT: tasks`;
      case 'link':
        return $localize`:Button that links two tasks as a review suggests:Link tasks`;
      case 'metadata':
        return $localize`:Button that applies the fields a review suggests:Apply to task`;
      default:
        return $localize`:Button that applies a review suggestion to the task:Apply to description`;
    }
  });

  private readonly keptSplitTasks = computed<AiReviewSplitTask[]>(() => {
    return this.splitRows()
      .filter((row) => row.kept && row.name.trim().length > 0)
      .map((row) => ({ name: row.name.trim(), description: row.description }));
  });

  private readonly keptTags = computed(() => {
    return this.tagRows()
      .filter((row) => row.kept && row.name.trim().length > 0)
      .map((row) => row.name.trim());
  });

  private readonly hasMetadataChoice = computed(() => {
    const metadata = this.metadata();
    const keepsEstimate =
      this.applyEstimate() && this.suggestedEstimate() !== null;
    const keepsPriority = this.applyPriority() && !!metadata?.priority;

    return this.keptTags().length > 0 || keepsEstimate || keepsPriority;
  });

  setSplitKept(index: number, kept: boolean) {
    this.splitRows.update((rows) => {
      return rows.map((row, at) => (at === index ? { ...row, kept } : row));
    });
  }

  renameSplit(index: number, name: string) {
    this.splitRows.update((rows) => {
      return rows.map((row, at) => (at === index ? { ...row, name } : row));
    });
  }

  setTagKept(index: number, kept: boolean) {
    this.tagRows.update((rows) => {
      return rows.map((row, at) => (at === index ? { ...row, kept } : row));
    });
  }

  renameTag(index: number, name: string) {
    this.tagRows.update((rows) => {
      return rows.map((row, at) => (at === index ? { ...row, name } : row));
    });
  }

  confirm() {
    this.dialogRef.close(this.result());
  }

  private result(): AiReviewAcceptDialogResult {
    switch (this.data.suggestion.kind) {
      case 'edit': {
        const replacement = this.replacement();
        const isUnchanged = replacement === this.edit()?.replace;

        return { replace: isUnchanged ? null : replacement };
      }
      case 'split':
        return { splitTasks: this.keptSplitTasks() };
      case 'metadata':
        return {
          tags: this.keptTags(),
          applyEstimate: this.applyEstimate(),
          applyPriority: this.applyPriority(),
        };
      default:
        return {};
    }
  }

  private initialSplitRows(): SplitRow[] {
    const suggestion = this.data.suggestion;

    if (suggestion.kind !== 'split') return [];

    return suggestion.tasks.map((task) => ({
      name: task.name,
      description: task.description ?? null,
      kept: true,
    }));
  }

  private initialTagRows(): TagRow[] {
    const suggestion = this.data.suggestion;

    if (suggestion.kind !== 'metadata') return [];

    const canCreateTags = this.data.canCreateTags;
    const taskTags = new Set(
      this.data.task.tags.map((tag) => tag.toLowerCase())
    );
    const tagsToAdd = suggestion.tags.filter((tag) => {
      return !taskTags.has(tag.name.toLowerCase());
    });

    return tagsToAdd.map((tag) => ({
      name: tag.name,
      isNew: tag.isNew,
      kept: !tag.isNew || canCreateTags,
    }));
  }
}
