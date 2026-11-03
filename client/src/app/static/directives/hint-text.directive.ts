import { Directive } from '@angular/core';

@Directive({
  selector: '[appHintText]',
  host: { class: 'text-muted text-xs' },
})
export class HintTextDirective {}
