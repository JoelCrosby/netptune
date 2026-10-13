import {
  Component,
  computed,
  input,
  linkedSignal,
  output,
} from '@angular/core';
import {
  EstimateType,
  estimateTypeOptions,
  tShirtSizes,
} from '@core/enums/estimate-type';
import { taskPriorityOptions } from '@core/enums/task-priority';
import {
  AiChangeEditorKind,
  AiChangeField,
} from '@core/models/ai-conversation';
import { AiChangeFieldEdit } from '@core/services/ai-api.service';
import { LucideTriangleAlert } from '@lucide/angular';
import { ButtonComponent } from '@static/components/button/button.component';
import { CalloutComponent } from '@static/components/callout/callout.component';
import { DatePickerComponent } from '@static/components/date-picker/date-picker.component';
import { FormControlFieldComponent } from '@static/components/form-control/form-control-field.component';
import { FormControlInputDirective } from '@static/components/form-control/form-control.directives';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { NumberInputComponent } from '@static/components/number-input/number-input.component';
import { AutofocusDirective } from '@static/directives/autofocus.directive';
import { fieldLabel } from './ai-assistant-change-group';
import { AiAssistantBoardGroupPickerComponent } from './ai-assistant-board-group-picker.component';
import {
  AiFieldDraft,
  ESTIMATE_TYPE_NAMES,
  PRIORITY_NAMES,
  initialDraft,
  isDraftComplete,
  isSameDraft,
  toFieldEdit,
} from './ai-assistant-field-edit';
import { AiAssistantMemberPickerComponent } from './ai-assistant-member-picker.component';
import { AiAssistantSprintPickerComponent } from './ai-assistant-sprint-picker.component';
import { AiAssistantStatusPickerComponent } from './ai-assistant-status-picker.component';
import { AiAssistantTagPickerComponent } from './ai-assistant-tag-picker.component';

const priorityOptions = taskPriorityOptions.map((option) => {
  return { value: PRIORITY_NAMES[option.value], label: option.label };
});

const estimateUnitOptions = estimateTypeOptions.map((option) => {
  return { value: ESTIMATE_TYPE_NAMES[option.value], label: option.label };
});

@Component({
  selector: 'app-ai-assistant-field-editor',
  host: {
    class: 'flex flex-col gap-2 px-2.5 py-2.5',
    '(keydown.escape)': 'cancel($event)',
  },
  imports: [
    ButtonComponent,
    CalloutComponent,
    DatePickerComponent,
    FormControlFieldComponent,
    FormControlInputDirective,
    FormSelectComponent,
    FormSelectOptionComponent,
    NumberInputComponent,
    AutofocusDirective,
    AiAssistantBoardGroupPickerComponent,
    AiAssistantMemberPickerComponent,
    AiAssistantSprintPickerComponent,
    AiAssistantStatusPickerComponent,
    AiAssistantTagPickerComponent,
  ],
  template: `
    @switch (kind()) {
      @case (kinds.priority) {
        <app-form-select
          [label]="label()"
          [noMargin]="true"
          [value]="draft().value"
          (changed)="setValue($event)">
          @for (option of priorityOptions; track option.value) {
            <app-form-select-option [value]="option.value">
              {{ option.label }}
            </app-form-select-option>
          }
        </app-form-select>
      }
      @case (kinds.estimate) {
        <div class="grid gap-2 sm:grid-cols-2">
          <app-form-select
            i18n-label="Label of the field that picks an estimate's unit"
            label="Unit"
            [noMargin]="true"
            [value]="draft().value"
            (changed)="setEstimateUnit($event)">
            @if (isClearable()) {
              <app-form-select-option [value]="null">
                {{ noEstimateLabel }}
              </app-form-select-option>
            }
            @for (option of estimateUnitOptions; track option.value) {
              <app-form-select-option [value]="option.value">
                {{ option.label }}
              </app-form-select-option>
            }
          </app-form-select>
          @if (isTShirt()) {
            <app-form-select
              i18n-label="Label of the field that picks a t-shirt size"
              label="Size"
              [noMargin]="true"
              [value]="draft().number"
              (changed)="setNumber($event)">
              @for (size of tShirtSizes; track size.value) {
                <app-form-select-option [value]="size.value">
                  {{ size.label }}
                </app-form-select-option>
              }
            </app-form-select>
          } @else if (draft().value !== null) {
            <app-number-input
              i18n-ariaLabel="Accessible label for an estimate's value"
              ariaLabel="Estimate"
              [min]="0"
              [step]="0.5"
              [value]="draft().number"
              (valueChange)="setNumber($event)" />
          }
        </div>
      }
      @case (kinds.date) {
        <div class="flex items-center gap-2">
          <app-date-picker
            class="min-w-0 flex-1"
            [label]="label()"
            [value]="draft().value ?? ''"
            (valueChange)="setValue($event || null)" />
          @if (isClearable() && draft().value !== null) {
            <app-button
              color="neutral"
              class="h-10 px-3"
              (click)="setValue(null)">
              <span i18n="Button that removes a date from a proposed change">
                Clear
              </span>
            </app-button>
          }
        </div>
      }
      @case (kinds.status) {
        <app-ai-assistant-status-picker
          [label]="label()"
          [value]="draft().value"
          [isClearable]="isClearable()"
          (valueChange)="setValue($event)" />
      }
      @case (kinds.user) {
        <app-ai-assistant-member-picker
          [label]="label()"
          [value]="draft().value"
          [isClearable]="isClearable()"
          (valueChange)="setValue($event)" />
      }
      @case (kinds.users) {
        <app-ai-assistant-member-picker
          [label]="label()"
          [values]="draft().values"
          [isMultiple]="true"
          (valuesChange)="setValues($event)" />
      }
      @case (kinds.tags) {
        <app-ai-assistant-tag-picker
          [label]="label()"
          [values]="draft().values"
          [proposedNames]="proposedTags()"
          (valuesChange)="setValues($event)" />
      }
      @case (kinds.sprint) {
        <app-ai-assistant-sprint-picker
          [label]="label()"
          [value]="draft().value"
          [projectId]="projectId()"
          [isClearable]="isClearable()"
          (valueChange)="setValue($event)" />
      }
      @case (kinds.boardGroup) {
        <app-ai-assistant-board-group-picker
          [label]="label()"
          [value]="draft().value"
          [projectId]="projectId()"
          [isClearable]="isClearable()"
          (valueChange)="setValue($event)" />
      }
      @default {
        <app-form-control-field class="px-2.5 py-1">
          <textarea
            appFormInput
            appAutofocus
            class="resize-y py-1.5 text-[15px] leading-relaxed!"
            [rows]="kind() === kinds.markdown ? 10 : 5"
            [value]="draft().value ?? ''"
            [disabled]="isSaving()"
            i18n-aria-label="
              Accessible label for the field that rewrites a proposed value
            "
            aria-label="Proposed value"
            (input)="setText($event)"></textarea>
        </app-form-control-field>
      }
    }

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
        (click)="cancelled.emit()">
        <span i18n="Button that abandons an edit to a proposed value">
          Cancel
        </span>
      </app-button>
      <app-button
        variant="outlined"
        color="primary"
        class="h-9 px-3.5 text-sm"
        [disabled]="!canSave()"
        (click)="save()">
        <span i18n="Button that keeps an edit to a proposed value"> Save </span>
      </app-button>
    </div>
  `,
})
export class AiAssistantFieldEditorComponent {
  readonly field = input.required<AiChangeField>();
  readonly proposedTags = input<string[]>([]);
  readonly isSaving = input(false);
  readonly error = input<string | null>(null);

  readonly saved = output<AiChangeFieldEdit>();
  readonly cancelled = output();

  protected readonly kinds = AiChangeEditorKind;
  protected readonly priorityOptions = priorityOptions;
  protected readonly estimateUnitOptions = estimateUnitOptions;
  protected readonly tShirtSizes = tShirtSizes;
  protected readonly alertIcon = LucideTriangleAlert;
  protected readonly noEstimateLabel = $localize`:Shown in place of an estimate a change removes:No estimate`;

  private readonly initial = computed(() => initialDraft(this.field()));

  protected readonly draft = linkedSignal<AiFieldDraft>(() => this.initial());

  protected readonly label = computed(() => fieldLabel(this.field().name));

  protected readonly kind = computed(() => {
    return this.field().editor?.kind ?? AiChangeEditorKind.text;
  });

  protected readonly isClearable = computed(() => {
    return this.field().editor?.isClearable ?? false;
  });

  protected readonly projectId = computed(() => {
    return this.field().editor?.projectId ?? null;
  });

  protected readonly isTShirt = computed(() => {
    return this.draft().value === ESTIMATE_TYPE_NAMES[EstimateType.tShirt];
  });

  protected readonly canSave = computed(() => {
    const editor = this.field().editor;
    const draft = this.draft();

    if (!editor || this.isSaving()) {
      return false;
    }

    const isChanged = !isSameDraft(draft, this.initial());

    return isChanged && isDraftComplete(editor, draft);
  });

  protected setValue(value: string | null) {
    this.draft.update((draft) => ({ ...draft, value }));
  }

  protected setValues(values: string[]) {
    this.draft.update((draft) => ({ ...draft, values }));
  }

  protected setNumber(number: number | null) {
    this.draft.update((draft) => ({ ...draft, number }));
  }

  protected setText(event: Event) {
    this.setValue((event.target as HTMLTextAreaElement).value);
  }

  // A t-shirt size and a point count share the value, so switching unit starts the value again.
  protected setEstimateUnit(value: string | null) {
    const wasTShirt = this.isTShirt();
    const isTShirt = value === ESTIMATE_TYPE_NAMES[EstimateType.tShirt];
    const keepsScale = wasTShirt === isTShirt;

    this.draft.update((draft) => {
      return { ...draft, value, number: keepsScale ? draft.number : null };
    });
  }

  protected save() {
    const editor = this.field().editor;

    if (!editor) {
      return;
    }

    this.saved.emit(toFieldEdit(this.field().name, editor, this.draft()));
  }

  // Escape belongs to the editor while it is open, not to the dialog behind it.
  protected cancel(event: Event) {
    event.stopPropagation();
    this.cancelled.emit();
  }
}
