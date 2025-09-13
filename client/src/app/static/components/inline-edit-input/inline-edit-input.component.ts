import { ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, HostBinding, Input, OnDestroy, OnInit, ViewChild, inject, input, output } from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { DocumentService } from '@static/services/document.service';
import { BehaviorSubject, Subject } from 'rxjs';
import { debounceTime, first, tap, takeUntil } from 'rxjs/operators';
import { AsyncPipe } from '@angular/common';

@Component({
    selector: 'app-inline-edit-input',
    templateUrl: './inline-edit-input.component.html',
    styleUrls: ['./inline-edit-input.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FormsModule, ReactiveFormsModule, AsyncPipe]
})
export class InlineEditInputComponent implements OnInit, OnDestroy {
  private elementRef = inject(ElementRef);
  private cd = inject(ChangeDetectorRef);
  private document = inject(DocumentService);

  @Input() value!: string | null | undefined;
  readonly size = input<number>();
  readonly activeBorder = input<boolean | string | null>();

  @ViewChild('input', { static: false }) input!: ElementRef;
  @HostBinding('class.edit-active') editActiveClass!: boolean;
  readonly submitted = output<string>();

  control = new FormControl('', {
    updateOn: 'blur',
  });

  isEditActiveSubject = new BehaviorSubject<boolean>(false);
  isEditActive$ = this.isEditActiveSubject.pipe(
    tap((value) => (this.editActiveClass = value))
  );

  onDestroy$ = new Subject<void>();

  ngOnInit() {
    this.document.documentClicked().subscribe({
      next: this.handleDocumentClick.bind(this),
    });

    this.control.valueChanges
      .pipe(
        takeUntil(this.onDestroy$),
        tap((value) => this.onSubmit(value as string))
      )
      .subscribe();
  }

  ngOnDestroy() {
    this.onDestroy$.next();
    this.onDestroy$.complete();
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
      this.control.setValue(this.value as string, { emitEvent: false });
      this.input?.nativeElement.focus();
    }
  }

  onSubmit(value: string) {
    this.submitted.emit(value);
    this.value = value;
    this.isEditActiveSubject.next(false);
  }
}
