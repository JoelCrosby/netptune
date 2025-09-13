import { Directive, ElementRef, Input, inject } from '@angular/core';

@Directive({ selector: '[appAutofocus]' })
export class AutofocusDirective {
  private host = inject<ElementRef<HTMLElement>>(ElementRef);

  @Input() set appAutofocus(_: unknown) {
    setTimeout(() => this.host.nativeElement.focus(), 200);
  }
}
