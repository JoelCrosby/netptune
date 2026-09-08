import { Directive } from '@angular/core';

// The small uppercase label that heads a group of fields or rows. A directive rather
// than a component so the call site keeps whatever element the outline needs.
@Directive({
  selector: '[appSectionLabel]',
  host: { class: 'text-muted text-xs font-semibold tracking-wide uppercase' },
})
export class SectionLabelDirective {}
