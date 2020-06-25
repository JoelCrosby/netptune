import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  ElementRef,
  EventEmitter,
  HostBinding,
  Input,
  OnDestroy,
  OnInit,
  Output,
  ViewChild,
} from '@angular/core';
import { FormControl } from '@angular/forms';
import { DocumentService } from '@app/static/services/document.service';
import { BehaviorSubject, Subject } from 'rxjs';
import { debounceTime, first, tap } from 'rxjs/operators';

@Component({
  selector: 'app-inline-edit-input',
  templateUrl: './inline-edit-input.component.html',
  styleUrls: ['./inline-edit-input.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InlineEditInputComponent implements OnInit, OnDestroy {
  @Input() value: string;
  @ViewChild('input', { static: false }) input: ElementRef;
  @HostBinding('class.edit-active') editActiveClass: boolean;
  @Output() submitted = new EventEmitter<string>();

  control = new FormControl();

  isEditActiveSubject = new BehaviorSubject<boolean>(false);
  isEditActive$ = this.isEditActiveSubject.pipe(
    tap((value) => (this.editActiveClass = value))
  );

  onDestroy$ = new Subject();

  constructor(
    private elementRef: ElementRef,
    private cd: ChangeDetectorRef,
    private document: DocumentService
  ) {}

  ngOnInit() {
    this.document.documentClicked().subscribe({
      next: this.handleDocumentClick.bind(this),
    });
  }

  ngOnDestroy() {
    this.onDestroy$.complete();
    this.onDestroy$.unsubscribe();
  }

  handleDocumentClick(target: EventTarget) {
    this.isEditActive$.pipe(first(), debounceTime(100)).subscribe({
      next: (isEditActive) => {
        if (isEditActive) {
          if (!this.elementRef.nativeElement.contains(target)) {
            return this.isEditActiveSubject.next(false);
          }
        } else {
          if (this.elementRef.nativeElement.contains(target)) {
            this.isEditActiveSubject.next(true);
            this.focusInput();
          }
        }
      },
    });
  }

  focusInput() {
    this.cd.detectChanges();

    if (this.input) {
      this.control.setValue(this.value, { emitEvent: false });
      this.input?.nativeElement.focus();
    }
  }

  onSubmit() {
    this.submitted.emit(this.control.value);
  }
}
