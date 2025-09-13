import { CdkTextareaAutosize } from '@angular/cdk/text-field';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  ElementRef,
  forwardRef,
  HostBinding,
  Input,
  NgZone,
  OnDestroy,
  OnInit,
  Provider,
  ViewChild,
} from '@angular/core';
import { ControlContainer, ControlValueAccessor, FormControl, FormControlDirective, NG_VALUE_ACCESSOR, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { DocumentService } from '@static/services/document.service';
import { BehaviorSubject, Subject } from 'rxjs';
import { debounceTime, first, takeUntil, tap } from 'rxjs/operators';
import { NgIf, AsyncPipe } from '@angular/common';

export const INLINE_TEXTAREA_VALUE_ACCESSOR: Provider = {
  provide: NG_VALUE_ACCESSOR,
  useExisting: forwardRef(() => InlineTextAreaComponent),
  multi: true,
};

@Component({
    selector: 'app-inline-text-area',
    templateUrl: './inline-text-area.component.html',
    styleUrls: ['./inline-text-area.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [INLINE_TEXTAREA_VALUE_ACCESSOR],
    imports: [NgIf, CdkTextareaAutosize, FormsModule, ReactiveFormsModule, AsyncPipe]
})
export class InlineTextAreaComponent
  implements OnInit, OnDestroy, ControlValueAccessor
{
  @Input() value!: string;
  @Input() formControlName!: string;
  @Input() formControl!: FormControl;
  @Input() activeBorder!: boolean | string;

  @Input() minRows = 1;
  @Input() maxRows = 3;

  @ViewChild('autosize') autosize!: CdkTextareaAutosize;
  @ViewChild('textarea', { static: false }) textarea!: ElementRef;
  @ViewChild(FormControlDirective, { static: false })
  controlDirective!: FormControlDirective;

  @HostBinding('class.edit-active') editActiveClass!: boolean;

  zone!: NgZone;
  onChange!: (value: string) => void;
  onTouched!: () => void;

  get control(): FormControl {
    return (
      this.formControl ||
      (this.controlContainer.control?.get(this.formControlName) as FormControl)
    );
  }

  isEditActiveSubject = new BehaviorSubject<boolean>(false);
  isEditActive$ = this.isEditActiveSubject.pipe(
    tap((value) => (this.editActiveClass = value))
  );

  onDestroy$ = new Subject<void>();

  constructor(
    private elementRef: ElementRef,
    private controlContainer: ControlContainer,
    private cd: ChangeDetectorRef,
    private document: DocumentService
  ) {}

  ngOnInit() {
    this.document.documentClicked().subscribe({
      next: this.handleDocumentClick.bind(this),
    });

    this.control.valueChanges
      .pipe(
        takeUntil(this.onDestroy$),
        tap((value: string) => this.change(value))
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
    this.autosize.resizeToFitContent(true);

    if (this.textarea) {
      this.textarea?.nativeElement.focus();
    }
  }

  change(value: string) {
    this.onChange(value);
  }

  /* eslint-disable @typescript-eslint/no-explicit-any */

  registerOnTouched(fn: any) {
    this.controlDirective?.valueAccessor?.registerOnTouched(fn);
  }

  registerOnChange(fn: any) {
    this.controlDirective?.valueAccessor?.registerOnChange(fn);
  }

  writeValue(obj: any) {
    this.controlDirective?.valueAccessor?.writeValue(obj);
  }

  /* eslint-enable @typescript-eslint/no-explicit-any */

  setDisabledState(isDisabled: boolean) {
    if (!this.controlDirective?.valueAccessor?.setDisabledState) {
      return;
    }

    this.controlDirective.valueAccessor.setDisabledState(isDisabled);
  }
}
