import { Directive, ElementRef, Input } from '@angular/core';

@Directive({
  selector: '[appAutofocus]',
})
export class AutofocusDirective {
  @Input() set appAutofocus(_: unknown) {
    setTimeout(() => this.host.nativeElement.focus(), 200);
  }

  constructor(private host: ElementRef<HTMLDListElement>) {}
}
