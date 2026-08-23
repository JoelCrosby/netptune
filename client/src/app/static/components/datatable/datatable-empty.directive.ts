import { Directive, TemplateRef, inject } from '@angular/core';

@Directive({
  selector: 'ng-template[appDatatableEmpty]',
})
export class DatatableEmptyDirective {
  readonly templateRef = inject<TemplateRef<unknown>>(TemplateRef);
}
