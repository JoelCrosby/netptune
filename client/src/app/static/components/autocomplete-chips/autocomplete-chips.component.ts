import { COMMA, ENTER } from '@angular/cdk/keycodes';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  input,
  model,
  output,
  viewChild,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import {
  MatAutocomplete,
  MatAutocompleteSelectedEvent,
  MatAutocompleteTrigger,
  MatOption,
} from '@angular/material/autocomplete';
import {
  MatChipGrid,
  MatChipInput,
  MatChipInputEvent,
  MatChipRemove,
  MatChipRow,
} from '@angular/material/chips';
import { MatIcon } from '@angular/material/icon';
import { MatLabel } from '@angular/material/input';
import { filterStringArray } from '@core/util/arrays';

export interface AutocompleteChipsSelectionChanged {
  type: 'Added' | 'Removed';
  option: string;
}

@Component({
  selector: 'app-autocomplete-chips',
  templateUrl: './autocomplete-chips.component.html',
  styleUrls: ['./autocomplete-chips.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatLabel,
    MatChipGrid,
    MatChipRow,
    MatIcon,
    MatChipRemove,
    FormsModule,
    MatAutocompleteTrigger,
    MatChipInput,
    ReactiveFormsModule,
    MatAutocomplete,
    MatOption,
  ],
})
export class AutocompleteChipsComponent {
  readonly placeholder = input.required<string>();
  readonly label = input<string | null>();
  readonly options = input.required<string[] | null>();
  readonly selected = model<string[] | null>([]);

  readonly matAutocomplete = viewChild.required<MatAutocomplete>('auto');
  readonly input = viewChild.required<ElementRef>('input');
  readonly autoTrigger = viewChild.required(MatAutocompleteTrigger);

  readonly selectionChanged = output<AutocompleteChipsSelectionChanged>();

  visible = true;
  selectable = true;
  removable = true;
  addOnBlur = false;

  separatorKeysCodes = [ENTER, COMMA];

  formCtrl = new FormControl();

  valueChanges = toSignal(this.formCtrl.valueChanges);
  filteredOptions = computed(() => {
    const option = this.valueChanges();
    const values = this.filter(option);

    return values.filter((value) => !this.selected()?.includes(value));
  });

  add(event: MatChipInputEvent) {
    const input = event.chipInput.inputElement;
    const value = event.value;

    if ((value || '').trim()) {
      const newOption = value.trim();

      const selectedSet = new Set(this.selected());

      if (selectedSet.has(newOption)) {
        input.value = '';
        this.formCtrl.setValue(null);
        return;
      }

      this.selectionChanged.emit({
        type: 'Added',
        option: newOption,
      });

      this.selected.set([...(this.selected() ?? []), newOption]);
    }

    if (input) {
      input.value = '';
    }

    this.formCtrl.setValue(null);
  }

  remove(option: string) {
    const selected = this.selected();
    if (!selected) return;

    const index = selected.indexOf(option);

    if (index >= 0) {
      this.selectionChanged.emit({
        type: 'Removed',
        option,
      });

      this.formCtrl.setValue('');
      this.selected.set(selected.filter((opt) => opt !== option));
    }
  }

  filter(name: string | null) {
    if (name === null) {
      return [];
    }

    return filterStringArray(this.options(), name);
  }

  onSelected(event: MatAutocompleteSelectedEvent): void {
    const newOption = event.option.viewValue;

    const selectedSet = new Set(this.selected());

    if (selectedSet.has(newOption)) {
      this.input().nativeElement.value = '';
      this.formCtrl.setValue(null);
      return;
    }

    this.selected.set([...(this.selected() ?? []), newOption]);

    this.selectionChanged.emit({
      type: 'Added',
      option: newOption,
    });

    this.input().nativeElement.value = '';
    this.formCtrl.setValue(null);
    requestAnimationFrame(() => this.autoTrigger().openPanel());
  }
}
