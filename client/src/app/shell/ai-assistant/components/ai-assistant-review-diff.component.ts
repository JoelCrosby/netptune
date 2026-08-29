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
import { AutofocusDirective } from '@static/directives/autofocus.directive';
import { fieldLabel } from './ai-assistant-change-group';
import {
  AiDiffMode,
  diffStat,
  lineOps,
  splitRows,
  unifiedLines,
  wordSegments,
} from './ai-assistant-diff';

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
  ],
  template: `
    <div class="border-border bg-card overflow-hidden rounded-lg border">
      <div
        class="border-border bg-card-header flex items-center gap-2.5 border-b px-3 py-2">
        <span class="font-avatar text-sm">{{ label() }}</span>
        <span class="text-muted font-avatar text-[13px]">{{
          stat().label
        }}</span>
        <span class="flex-1"></span>
        @if (isEditing()) {
          <span
            class="text-muted text-[13px]"
            i18n="Shown on the field of a change while its value is rewritten">
            Editing
          </span>
        } @else {
          <span class="text-muted text-[13px]">{{ modeLabel() }}</span>
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
        @switch (mode()) {
          @case ('split') {
            <div
              class="font-avatar grid grid-cols-[minmax(0,1fr)_1px_minmax(0,1fr)] text-[15px] leading-relaxed">
              <div>
                <div
                  class="border-border text-muted border-b px-3 py-2 text-xs tracking-wide uppercase"
                  i18n="Column heading above the current value of a field">
                  Before
                </div>
                @for (row of rows(); track $index) {
                  <div
                    class="flex gap-3 px-3 break-words whitespace-pre-wrap"
                    [class.bg-diff-del]="row.beforeKind === 'removed'"
                    [class.bg-hover]="row.beforeKind === null">
                    <span
                      class="text-muted w-7 shrink-0 text-right select-none">
                      {{ row.beforeNumber }}
                    </span>
                    <span class="text-muted min-w-0">{{ row.before }}</span>
                  </div>
                }
              </div>

              <div class="bg-border"></div>

              <div>
                <div
                  class="border-border text-muted border-b px-3 py-2 text-xs tracking-wide uppercase"
                  i18n="Column heading above the proposed value of a field">
                  After
                </div>
                @for (row of rows(); track $index) {
                  <div
                    class="flex gap-3 px-3 break-words whitespace-pre-wrap"
                    [class.bg-diff-add]="row.afterKind === 'added'"
                    [class.bg-hover]="row.afterKind === null">
                    <span
                      class="text-muted w-6 shrink-0 text-right select-none">
                      {{ row.afterNumber }}
                    </span>
                    <span class="min-w-0">{{ row.after }}</span>
                  </div>
                }
              </div>
            </div>
          }
          @case ('unified') {
            <div class="font-avatar py-1.5 text-[15px] leading-relaxed">
              @for (line of lines(); track $index) {
                <div
                  class="flex gap-3 px-3 break-words whitespace-pre-wrap"
                  [class.bg-diff-add]="line.kind === 'added'"
                  [class.bg-diff-del]="line.kind === 'removed'">
                  <span
                    class="w-3 shrink-0 select-none"
                    [class.text-change-added]="line.kind === 'added'"
                    [class.text-change-removed]="line.kind === 'removed'"
                    [class.text-transparent]="line.kind === 'context'">
                    {{ line.mark }}
                  </span>
                  <span
                    class="min-w-0"
                    [class.text-muted]="line.kind === 'removed'">
                    {{ line.text }}
                  </span>
                </div>
              }
            </div>
          }
          @default {
            <p
              class="m-0 px-4 py-3.5 text-[15px] leading-relaxed break-words whitespace-pre-wrap">
              @for (segment of segments(); track $index) {
                <span
                  class="rounded-sm"
                  [class.bg-diff-add-word]="segment.kind === 'added'"
                  [class.bg-diff-del-word]="segment.kind === 'removed'"
                  [class.text-muted]="segment.kind === 'removed'"
                  [class.line-through]="segment.kind === 'removed'">
                  {{ segment.value }}
                </span>
              }
            </p>
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

  protected readonly label = computed(() => fieldLabel(this.field().name));

  private readonly ops = computed(() => lineOps(this.field()));

  protected readonly rows = computed(() => splitRows(this.ops()));
  protected readonly lines = computed(() => unifiedLines(this.ops()));
  protected readonly segments = computed(() => wordSegments(this.field()));
  protected readonly stat = computed(() => diffStat(this.ops()));

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
