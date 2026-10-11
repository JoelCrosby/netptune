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
import { diffStat } from '@static/components/text-diff/text-diff';
import { TextDiffComponent } from '@static/components/text-diff/text-diff.component';
import { AiDiffMode, lineOps } from './ai-assistant-diff';

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
    TextDiffComponent,
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
        <app-text-diff
          [before]="field().before"
          [after]="field().after"
          [mode]="mode()" />
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
