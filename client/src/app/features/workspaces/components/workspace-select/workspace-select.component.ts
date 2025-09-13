import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  OnChanges,
  OnInit,
  Output,
  SimpleChanges,
  ViewChild,
} from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { logout } from '@core/auth/store/auth.actions';
import { Workspace } from '@core/models/workspace';
import { filterObjectArray } from '@core/util/arrays';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { Store } from '@ngrx/store';
import { BehaviorSubject, fromEvent } from 'rxjs';
import { debounceTime, filter, tap, throttleTime } from 'rxjs/operators';
import { NgIf, NgFor, AsyncPipe } from '@angular/common';
import { AutofocusDirective } from '../../../../static/directives/autofocus.directive';
import { RouterLink } from '@angular/router';

@UntilDestroy()
@Component({
    selector: 'app-workspace-select',
    templateUrl: './workspace-select.component.html',
    styleUrls: ['./workspace-select.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [NgIf, FormsModule, AutofocusDirective, ReactiveFormsModule, NgFor, RouterLink, AsyncPipe]
})
export class WorkspaceSelectComponent implements OnInit, OnChanges {
  @ViewChild('dropdown') dropdownElementRef!: ElementRef;

  @Input() options: Workspace[] | null = [];
  @Input() value?: string | null;
  @Input() compact = false;

  @Output() selectChange = new EventEmitter<Workspace>();
  @Output() closed = new EventEmitter();

  searchControl = new FormControl();

  isOpen = false;
  currentWorkspace: Workspace | null = null;
  selected: Workspace | null = null;

  options$ = new BehaviorSubject<Workspace[]>([]);

  constructor(private store: Store) {}

  ngOnInit() {
    this.searchControl.valueChanges
      .pipe(debounceTime(300), untilDestroyed(this))
      .subscribe((term: string) => this.search(term));

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

    this.options$.next(this.options ?? []);
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes.value || changes.options) {
      if (this.value && !this.currentWorkspace && this.options) {
        const option = this.options.find((opt) => opt.slug === this.value);
        this.select(option);
      }
    }
  }

  handleDocumentClick(event: Event) {
    if (!this.dropdownElementRef.nativeElement.contains(event.target)) {
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

    if (!this.options) return;

    if (!this.selected) {
      this.selected = (options.length && options[0]) || null;
    } else {
      const currentIndex = options.findIndex(
        (opt) => opt.id === this.selected?.id
      );

      if (currentIndex === 0) return;

      this.selected = this.options[currentIndex - 1];
    }

    this.options$.next(options);
  }

  open(dropdown: HTMLElement, origin: HTMLElement) {
    this.isOpen = true;
    if (this.compact) {
      dropdown.style.width = '200px';
      dropdown.style.left = '80px';
      dropdown.style.top = '0px';
    } else {
      dropdown.style.width = `${origin.offsetWidth}px`;
      dropdown.style.left = '0';
      dropdown.style.top = '55px';
    }
  }

  close() {
    this.closed.emit();
    this.isOpen = false;
    this.searchControl.patchValue('');
  }

  select(option: Workspace | null = null) {
    this.selected = option ?? this.selected;
    this.currentWorkspace = this.selected;

    if (this.isOpen && this.selected) {
      this.selectChange.emit(this.selected);
      this.close();
    }

    this.selected = null;
  }

  isActive(option: Workspace) {
    if (!this.selected) {
      return false;
    }
    return option.id === this.selected.id;
  }

  search(value: string) {
    if (!this.options) return;

    if (!value) {
      this.options$.next(this.options);
    } else {
      this.options$.next(filterObjectArray(this.options, 'name', value));
      this.selectNextOptiom();
    }
  }

  onlogOutClicked() {
    this.close();
    this.store.dispatch(logout());
  }
}
