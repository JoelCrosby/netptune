import { Overlay, OverlayConfig, OverlayRef } from '@angular/cdk/overlay';
import { CdkPortal } from '@angular/cdk/portal';
import {
  ChangeDetectionStrategy,
  Component,
  ContentChildren,
  ElementRef,
  EventEmitter,
  HostListener,
  Input,
  OnInit,
  Optional,
  Output,
  QueryList,
  Self,
  ViewChild,
} from '@angular/core';
import { ControlValueAccessor, NgControl } from '@angular/forms';

@Component({
  selector: 'app-form-select-option',
  template: `
    <div class="nept-form-select-option">
      <ng-content></ng-content>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FormSelectOptionComponent {
  @Input() value: unknown;
}

@Component({
  selector: 'app-form-select-dropdown',
  template: `
    <ng-template cdkPortal>
      <ng-content></ng-content>
    </ng-template>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FormSelectDropdownComponent {
  @Input() reference!: HTMLElement;
  @ViewChild(CdkPortal) portal!: CdkPortal;

  constructor(private overlay: Overlay) {}

  overlayRef!: OverlayRef;
  showing = false;

  show() {
    console.log({ portal: this.portal });

    this.overlayRef = this.overlay.create(this.getOverlayConfig());
    this.overlayRef.attach(this.portal);
    this.syncWidth();
    this.overlayRef.backdropClick().subscribe(() => this.hide());
    this.showing = true;
  }

  hide() {
    this.overlayRef.detach();
    this.showing = false;
  }

  @HostListener('window:resize')
  onWinResize() {
    this.syncWidth();
  }

  private syncWidth() {
    if (!this.overlayRef) {
      return;
    }

    const refRect = this.reference.getBoundingClientRect();
    this.overlayRef.updateSize({ width: refRect.width });
  }

  private getOverlayConfig(): OverlayConfig {
    const positionStrategy = this.overlay
      .position()
      .flexibleConnectedTo(this.reference)
      .withPush(false)
      .withPositions([
        {
          originX: 'start',
          originY: 'bottom',
          overlayX: 'start',
          overlayY: 'top',
        },
        {
          originX: 'start',
          originY: 'top',
          overlayX: 'start',
          overlayY: 'bottom',
        },
      ]);

    return new OverlayConfig({
      positionStrategy: positionStrategy,
      hasBackdrop: true,
      backdropClass: 'cdk-overlay-transparent-backdrop',
    });
  }
}

@Component({
  selector: 'app-form-select',
  templateUrl: './form-select.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FormSelectComponent implements OnInit, ControlValueAccessor {
  @Input() label!: string;
  @Input() disabled!: boolean;
  @Input() icon!: string;
  @Input() prefix!: string;
  @Input() autocomplete = 'off';
  @Input() placeholder?: string;
  @Input() hint?: string;
  @Input() minLength?: string;
  @Input() maxLength?: string;

  @ViewChild('input') input!: ElementRef;

  @ViewChild(FormSelectDropdownComponent)
  public dropdown!: FormSelectDropdownComponent;

  @ContentChildren(FormSelectOptionComponent, { descendants: true })
  options!: QueryList<FormSelectOptionComponent>;

  @Output() submitted = new EventEmitter<string>();

  value?: string | number;

  onChange!: (value: string) => void;
  onTouch!: (...args: unknown[]) => void;

  get control() {
    return this.ngControl.control;
  }

  constructor(
    @Self()
    @Optional()
    public ngControl: NgControl
  ) {
    if (this.ngControl) {
      this.ngControl.valueAccessor = this;
    }
  }

  ngOnInit() {
    console.log('OnInit');
  }

  showDropdown() {
    this.dropdown.show();
  }

  onDropMenuIconClick(event: UIEvent) {
    event.stopPropagation();
    setTimeout(() => {
      this.input.nativeElement.focus();
      this.input.nativeElement.click();
    }, 10);
  }

  onInputchange(event: Event) {
    const target = event.target as HTMLInputElement;
    const value = target.value;

    this.onChange(value);
    this.onTouch();
  }

  writeValue(value: string) {
    this.value = value;
  }

  registerOnChange(fn: (...args: unknown[]) => unknown) {
    this.onChange = fn;
  }

  registerOnTouched(fn: (...args: unknown[]) => unknown) {
    this.onTouch = fn;
  }

  setDisabledState(isDisabled: boolean) {
    this.disabled = isDisabled;
  }
}
