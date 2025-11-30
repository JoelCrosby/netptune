import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnChanges,
  OnInit,
  SimpleChanges,
  input,
  output,
  viewChild,
} from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { AppUser } from '@core/models/appuser';
import { AssigneeViewModel } from '@core/models/view-models/board-view';
import { filterObjectArray } from '@core/util/arrays';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { BehaviorSubject, fromEvent } from 'rxjs';
import { debounceTime, filter, tap, throttleTime } from 'rxjs/operators';
import { AsyncPipe } from '@angular/common';
import { AvatarComponent } from '../avatar/avatar.component';
import { AutofocusDirective } from '../../directives/autofocus.directive';

@UntilDestroy()
@Component({
  selector: 'app-user-select',
  templateUrl: './user-select.component.html',
  styleUrls: ['./user-select.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    AvatarComponent,
    FormsModule,
    AutofocusDirective,
    ReactiveFormsModule,
    AsyncPipe,
  ],
})
export class UserSelectComponent implements OnInit, OnChanges {
  readonly dropdownElementRef = viewChild.required<ElementRef>('dropdown');

  readonly options = input<AppUser[] | null>([]);
  readonly value = input<((AssigneeViewModel | AppUser)[] | null) | undefined>(
    []
  );
  readonly compact = input(false);
  readonly label = input('Select Users');
  readonly noResults = input('No results found...');

  readonly selectChange = output<AppUser>();
  readonly closed = output();

  searchControl = new FormControl('');

  isOpen = false;
  selected: AppUser | null = null;

  options$ = new BehaviorSubject<AppUser[]>([]);
  valueIdSet = new Set<string>();

  ngOnInit() {
    this.searchControl.valueChanges
      .pipe(debounceTime(300), untilDestroyed(this))
      .subscribe((term) => this.search(term));

    fromEvent(document, 'mousedown', {
      passive: true,
    })
      .pipe(
        untilDestroyed(this),
        throttleTime(200),
        tap(this.handleDocumentClick.bind(this))
      )
      .subscribe();

    fromEvent<KeyboardEvent>(document, 'keydown', {
      passive: true,
    })
      .pipe(
        untilDestroyed(this),
        filter(() => this.isOpen),
        tap(this.handleKeyDown.bind(this))
      )
      .subscribe();

    this.options$.next(this.options() ?? []);
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes.value || changes.options) {
      const options = this.options();
      const value = this.value();
      if (value && options) {
        this.options$.next(options);

        this.valueIdSet.clear();

        for (const user of value) {
          user && this.valueIdSet.add(user.id);
        }
      }
    }
  }

  handleDocumentClick(event: Event) {
    if (!this.dropdownElementRef().nativeElement.contains(event.target)) {
      this.close();
    }
  }

  handleKeyDown(event: KeyboardEvent) {
    switch (event.key) {
      case 'ArrowUp':
        this.selectPreviousOption();
        break;
      case 'ArrowDown':
        this.selectNextOptiom();
        break;
      case 'Enter':
        this.select();
        break;
    }
  }

  selectNextOptiom() {
    const options = this.options$.value;

    if (!this.selected) {
      this.selected = (options.length && options[0]) || null;
    } else {
      const currentIndex = options.findIndex(
        (opt) => opt.id === this.selected?.id
      );

      if (options.length === currentIndex + 1) {
        return;
      }

      this.selected = options[currentIndex + 1];
    }

    this.options$.next(options);
  }

  selectPreviousOption() {
    const options = this.options$.value;

    const optionsValue = this.options();
    if (!optionsValue) return;

    if (!this.selected) {
      this.selected = (options.length && options[0]) || null;
    } else {
      const currentIndex = options.findIndex(
        (opt) => opt.id === this.selected?.id
      );

      if (currentIndex === 0) return;

      this.selected = optionsValue[currentIndex - 1];
    }

    this.options$.next(options);
  }

  open(dropdown: HTMLElement, origin: HTMLElement) {
    this.isOpen = true;
    if (this.compact()) {
      dropdown.style.width = '200px';
      dropdown.style.left = '80px';
      dropdown.style.top = '0px';
    } else {
      dropdown.style.width = `${origin.offsetWidth}px`;
      dropdown.style.left = '0';
      dropdown.style.top = '42px';
    }
  }

  close() {
    this.closed.emit();
    this.isOpen = false;
    this.searchControl.patchValue('');
  }

  select(option: AppUser | null = null) {
    this.selected = option ?? this.selected;

    if (this.isOpen && this.selected) {
      this.selectChange.emit(this.selected);
    }

    this.selected = null;
  }

  isActive(option: AppUser) {
    if (!this.selected) {
      return false;
    }
    return option.id === this.selected.id;
  }

  isSelected(option: AppUser) {
    return this.valueIdSet.has(option.id);
  }

  search(value?: string | null) {
    const options = this.options();
    if (!options) return;

    if (!value) {
      this.options$.next(options);
    } else {
      this.options$.next(filterObjectArray(options, 'displayName', value));
      this.selectNextOptiom();
    }
  }
}
