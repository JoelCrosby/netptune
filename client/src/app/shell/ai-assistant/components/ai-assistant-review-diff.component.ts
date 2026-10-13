import {
  Component,
  computed,
  input,
  linkedSignal,
  output,
} from '@angular/core';
import { AiChangeField } from '@core/models/ai-conversation';
import { LucidePencil, LucideTriangleAlert } from '@lucide/angular';
import { ButtonComponent } from '@static/components/button/button.component';
import { CalloutComponent } from '@static/components/callout/callout.component';
import { FormControlFieldComponent } from '@static/components/form-control/form-control-field.component';
import { FormControlInputDirective } from '@static/components/form-control/form-control.directives';
import {
  SegmentedControlComponent,
  SegmentedOption,
} from '@static/components/segmented-control/segmented-control.component';
import { TextDiffComponent } from '@static/components/text-diff/text-diff.component';
import { AutofocusDirective } from '@static/directives/autofocus.directive';
import { fieldLabel, isProseField } from './ai-assistant-change-group';
import { diffStat } from '@static/components/text-diff/text-diff';
import { valueDiff } from './ai-assistant-change-kind';
import { AiAssistantChoiceDiffComponent } from './ai-assistant-choice-diff.component';
import { AiAssistantDateDiffComponent } from './ai-assistant-date-diff.component';
import { AiDiffMode, lineOps } from './ai-assistant-diff';
import { AiAssistantEstimateDiffComponent } from './ai-assistant-estimate-diff.component';
import { fieldDiffKind } from './ai-assistant-field-diff';
import { AiAssistantMarkdownDiffComponent } from './ai-assistant-markdown-diff.component';
import { AiAssistantRelationDiffComponent } from './ai-assistant-relation-diff.component';
import { AiAssistantSetDiffComponent } from './ai-assistant-set-diff.component';
import { AiAssistantTextDiffComponent } from './ai-assistant-text-diff.component';

type AiMarkdownView = 'rendered' | 'source';

/** One field of a proposed change, rendered as a diff in the reviewer's chosen mode. */
@Component({
  selector: 'app-ai-assistant-review-diff',
  host: { class: 'block' },
  imports: [
    LucidePencil,
    ButtonComponent,
    CalloutComponent,
    FormControlFieldComponent,
    FormControlInputDirective,
    AutofocusDirective,
    SegmentedControlComponent,
    TextDiffComponent,
    AiAssistantChoiceDiffComponent,
    AiAssistantDateDiffComponent,
    AiAssistantEstimateDiffComponent,
    AiAssistantMarkdownDiffComponent,
    AiAssistantRelationDiffComponent,
    AiAssistantSetDiffComponent,
    AiAssistantTextDiffComponent,
  ],
  template: `
    <div class="border-border bg-card overflow-hidden rounded-lg border">
      <div
        class="border-border bg-card-header flex items-center gap-2.5 border-b px-3 py-2">
        <span class="font-avatar text-sm">{{ label() }}</span>
        @if (statLabel(); as statLabel) {
          <span class="text-muted font-avatar text-[13px]">{{
            statLabel
          }}</span>
        }
        <span class="flex-1"></span>
        @if (isEditing()) {
          <span
            class="text-muted text-[13px]"
            i18n="Shown on the field of a change while its value is rewritten">
            Editing
          </span>
        } @else {
          @if (usesMode()) {
            <span class="text-muted text-[13px]">{{ modeLabel() }}</span>
          }
          @if (diffKind() === 'markdown') {
            <app-segmented-control
              variant="subtle"
              class="-my-1"
              [options]="views"
              [(value)]="view"
              [ariaLabel]="viewGroupLabel" />
          }
          @if (canEdit()) {
            <app-button
              color="neutral"
              class="-my-1 h-8 gap-1.5 px-2.5 text-[13px]"
              (click)="editStarted.emit(field().name)">
              <svg lucidePencil class="h-3.5 w-3.5"></svg>
              <span i18n="Button that rewrites the value a change proposes">
                Edit
              </span>
            </app-button>
          }
        }
      </div>

      @if (isEditing()) {
        <div class="flex flex-col gap-2 px-2.5 py-2.5">
          <app-form-control-field class="px-2.5 py-1">
            <textarea
              appFormInput
              appAutofocus
              rows="5"
              class="resize-y py-1.5 text-[15px] leading-relaxed!"
              [value]="draft()"
              [disabled]="isSaving()"
              i18n-aria-label="
                Accessible label for the field that rewrites a proposed value
              "
              aria-label="Proposed value"
              (input)="setDraft($event)"
              (keydown.escape)="cancel($event)"></textarea>
          </app-form-control-field>

          @if (error(); as message) {
            <app-callout color="warn" role="alert" [icon]="alertIcon">
              <span class="text-sm">{{ message }}</span>
            </app-callout>
          }

          <div class="flex items-center justify-end gap-2">
            <app-button
              color="neutral"
              class="h-9 px-3.5 text-sm"
              [disabled]="isSaving()"
              (click)="editCancelled.emit()">
              <span i18n="Button that abandons an edit to a proposed value">
                Cancel
              </span>
            </app-button>
            <app-button
              variant="outlined"
              color="primary"
              class="h-9 px-3.5 text-sm"
              [disabled]="isSaving() || !isChanged()"
              (click)="saved.emit(draft())">
              <span i18n="Button that keeps an edit to a proposed value">
                Save
              </span>
            </app-button>
          </div>
        </div>
      } @else {
        @switch (diffKind()) {
          @case ('markdown') {
            @if (view() === 'source') {
              <app-text-diff
                [before]="field().before"
                [after]="field().after"
                [mode]="mode()" />
            } @else {
              <app-ai-assistant-markdown-diff
                [field]="field()"
                [mode]="mode()" />
            }
          }
          @case ('choice') {
            <app-ai-assistant-choice-diff [field]="field()" />
          }
          @case ('date') {
            <app-ai-assistant-date-diff [field]="field()" />
          }
          @case ('set') {
            <app-ai-assistant-set-diff [field]="field()" />
          }
          @case ('estimate') {
            <app-ai-assistant-estimate-diff [field]="field()" />
          }
          @case ('relation') {
            <app-ai-assistant-relation-diff [field]="field()" />
          }
          @default {
            <app-ai-assistant-text-diff [field]="field()" [mode]="mode()" />
          }
        }
      }
    </div>
  `,
})
export class AiAssistantReviewDiffComponent {
  readonly field = input.required<AiChangeField>();
  readonly mode = input<AiDiffMode>('split');
  readonly canEdit = input(false);
  readonly isEditing = input(false);
  readonly isSaving = input(false);
  readonly error = input<string | null>(null);

  readonly editStarted = output<string>();
  readonly editCancelled = output();
  readonly saved = output<string>();

  /** Reopening the editor, or moving to another change, starts from what is proposed now. */
  protected readonly draft = linkedSignal(() => {
    this.isEditing();

    return this.field().after ?? '';
  });

  protected readonly isChanged = computed(() => {
    return this.draft() !== (this.field().after ?? '');
  });

  protected readonly alertIcon = LucideTriangleAlert;

  protected readonly view = linkedSignal<AiChangeField, AiMarkdownView>({
    source: this.field,
    computation: () => 'rendered',
  });

  protected readonly views: SegmentedOption<AiMarkdownView>[] = [
    {
      value: 'rendered',
      label: $localize`:Shows a markdown diff as formatted text:Rendered`,
    },
    {
      value: 'source',
      label: $localize`:Raw markdown view|Shows a markdown diff as its raw markdown:Source`,
    },
  ];

  protected readonly viewGroupLabel = $localize`:Accessible label for the switch between formatted and raw markdown in a diff:Markdown view`;

  protected readonly label = computed(() => fieldLabel(this.field().name));

  protected readonly diffKind = computed(() => fieldDiffKind(this.field()));

  protected readonly usesMode = computed(() => {
    const kind = this.diffKind();

    return (
      kind === 'markdown' || (kind === 'text' && isProseField(this.field()))
    );
  });

  protected readonly statLabel = computed(() => {
    const kind = this.diffKind();

    if (kind === 'text' || kind === 'markdown') {
      return diffStat(lineOps(this.field())).label;
    }

    if (kind !== 'set') {
      return null;
    }

    const diff = valueDiff(this.field());

    return `+${diff.added.length} \u2212${diff.removed.length}`;
  });

  protected readonly modeLabel = computed(() => {
    const mode = this.mode();

    if (mode === 'split') {
      return $localize`:Names the side by side diff layout:Side by side`;
    }

    if (mode === 'unified') {
      return $localize`:Names the single column diff layout:Unified`;
    }

    return $localize`:Names the diff layout that highlights changed words:Word level`;
  });

  protected setDraft(event: Event) {
    this.draft.set((event.target as HTMLTextAreaElement).value);
  }

  /** Escape belongs to the editor while it is open, not to the dialog behind it. */
  protected cancel(event: Event) {
    event.stopPropagation();
    this.editCancelled.emit();
  }
}
