import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  OnDestroy,
  OnInit,
  Output,
  ViewChild,
} from '@angular/core';
import { FormControl } from '@angular/forms';
import { fromEvent, Subject } from 'rxjs';
import { takeUntil, tap, throttleTime } from 'rxjs/operators';

@Component({
  selector: 'app-tags-input',
  templateUrl: './tags-input.component.html',
  styleUrls: ['./tags-input.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TagsInputComponent implements OnInit, OnDestroy, AfterViewInit {
  @Input() value: string | null = null;
  @ViewChild('input') input!: ElementRef;

  @Output() submitted = new EventEmitter<string>();
  @Output() canceled = new EventEmitter();

  onDestroy$ = new Subject();
  formControl!: FormControl;

  constructor() {}

  ngOnInit() {
    this.formControl = new FormControl(this.value);

    fromEvent(document, 'mousedown', {
      passive: true,
    })
      .pipe(
        takeUntil(this.onDestroy$),
        throttleTime(200),
        tap(this.handleDocumentClick.bind(this))
      )
      .subscribe();
  }

  ngAfterViewInit() {
    this.input.nativeElement.focus();
  }

  ngOnDestroy() {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  onSubmit() {
    const value = this.formControl.value;
    this.submitted.emit(value);
  }

  handleDocumentClick(event: Event) {
    if (!this.input.nativeElement.contains(event.target)) {
      this.canceled.emit();
    }
  }
}
