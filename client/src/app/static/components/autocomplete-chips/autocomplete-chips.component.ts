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
} from '@angular/core';
import { FormControl } from '@angular/forms';
import {
  MatAutocomplete,
  MatAutocompleteSelectedEvent,
} from '@angular/material/autocomplete';
import { MatChipInputEvent } from '@angular/material/chips';
import { filterStringArray } from '@core/util/arrays';
import { Observable } from 'rxjs';
import { map, startWith } from 'rxjs/operators';

export interface AutocompleteChipsSelectionChanged {
  type: 'Added' | 'Removed';
  option: string;
}

@Component({
  selector: 'app-autocomplete-chips',
  templateUrl: './autocomplete-chips.component.html',
  styleUrls: ['./autocomplete-chips.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AutocompleteChipsComponent implements OnInit {
  @Input() placeholder: string;
  @Input() label: string;
  @Input() options: string[];
  @Input() selected: string[] = [];
  @ViewChild('auto') matAutocomplete: MatAutocomplete;

  @Output() selectionChanged = new EventEmitter<
    AutocompleteChipsSelectionChanged
  >();

  visible = true;
  selectable = true;
  removable = true;
  addOnBlur = false;

  separatorKeysCodes = [ENTER, COMMA];

  formCtrl = new FormControl();

  filteredOptions: Observable<string[]>;

  @ViewChild('input') input: ElementRef;

  ngOnInit() {
    this.filteredOptions = this.formCtrl.valueChanges.pipe(
      startWith(''),
      map((option: string | null) => this.filter(option)),
      map((values) => values.filter((value) => !this.selected.includes(value)))
    );
  }

  add(event: MatChipInputEvent) {
    const input = event.input;
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

      this.selected = [...this.selected, newOption];
    }

    if (input) {
      input.value = '';
    }

    this.formCtrl.setValue(null);
  }

  remove(option: string) {
    const index = this.selected.indexOf(option);

    if (index >= 0) {
      this.selectionChanged.emit({
        type: 'Removed',
        option,
      });

      this.selected = this.selected.filter((opt) => opt !== option);
    }
  }

  filter(name: string) {
    return filterStringArray(this.options, name);
  }

  onSelected(event: MatAutocompleteSelectedEvent): void {
    const newOption = event.option.viewValue;

    const selectedSet = new Set(this.selected);

    if (selectedSet.has(newOption)) {
      this.input.nativeElement.value = '';
      this.formCtrl.setValue(null);
      return;
    }

    this.selected = [...this.selected, newOption];

    this.selectionChanged.emit({
      type: 'Added',
      option: newOption,
    });

    this.input.nativeElement.value = '';
    this.formCtrl.setValue(null);
  }
}
