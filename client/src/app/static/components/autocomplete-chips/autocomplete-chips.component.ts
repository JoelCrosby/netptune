import { COMMA, ENTER } from '@angular/cdk/keycodes';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  OnInit,
  Output,
  ViewChild,
  input,
} from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import {
  MatAutocomplete,
  MatAutocompleteSelectedEvent,
  MatAutocompleteTrigger,
  MatOption,
} from '@angular/material/autocomplete';
import {
  MatChipInputEvent,
  MatChipGrid,
  MatChipRow,
  MatChipRemove,
  MatChipInput,
} from '@angular/material/chips';
import { filterStringArray } from '@core/util/arrays';
import { Observable } from 'rxjs';
import { map, startWith } from 'rxjs/operators';
import { AsyncPipe } from '@angular/common';
import { MatLabel } from '@angular/material/input';
import { MatIcon } from '@angular/material/icon';

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
    AsyncPipe,
  ],
})
export class AutocompleteChipsComponent implements OnInit {
  readonly placeholder = input.required<string>();
  readonly label = input<string | null>();
  readonly options = input.required<string[] | null>();
  @Input() selected: string[] | null = [];

  @ViewChild('auto') matAutocomplete!: MatAutocomplete;
  @ViewChild('input') input!: ElementRef;
  @ViewChild(MatAutocompleteTrigger) autoTrigger!: MatAutocompleteTrigger;

  @Output()
  selectionChanged = new EventEmitter<AutocompleteChipsSelectionChanged>();

  visible = true;
  selectable = true;
  removable = true;
  addOnBlur = false;

  separatorKeysCodes = [ENTER, COMMA];

  formCtrl = new FormControl();

  filteredOptions!: Observable<string[]>;

  ngOnInit() {
    this.filteredOptions = this.formCtrl.valueChanges.pipe(
      startWith<string>(''),
      map((option: string | null) => this.filter(option)),
      map((values) => values.filter((value) => !this.selected?.includes(value)))
    );
  }

  add(event: MatChipInputEvent) {
    const input = event.chipInput.inputElement;
    const value = event.value;

    if ((value || '').trim()) {
      const newOption = value.trim();

      const selectedSet = new Set(this.selected);

      if (selectedSet.has(newOption)) {
        input.value = '';
        this.formCtrl.setValue(null);
        return;
      }

      this.selectionChanged.emit({
        type: 'Added',
        option: newOption,
      });

      this.selected = [...(this.selected ?? []), newOption];
    }

    if (input) {
      input.value = '';
    }

    this.formCtrl.setValue(null);
  }

  remove(option: string) {
    if (!this.selected) return;

    const index = this.selected.indexOf(option);

    if (index >= 0) {
      this.selectionChanged.emit({
        type: 'Removed',
        option,
      });

      this.formCtrl.setValue('');
      this.selected = this.selected.filter((opt) => opt !== option);
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

    const selectedSet = new Set(this.selected);

    if (selectedSet.has(newOption)) {
      this.input.nativeElement.value = '';
      this.formCtrl.setValue(null);
      return;
    }

    this.selected = [...(this.selected ?? []), newOption];

    this.selectionChanged.emit({
      type: 'Added',
      option: newOption,
    });

    this.input.nativeElement.value = '';
    this.formCtrl.setValue(null);
    requestAnimationFrame(() => this.autoTrigger.openPanel());
  }
}
