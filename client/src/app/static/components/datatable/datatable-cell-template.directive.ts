import { Directive, TemplateRef, inject, input } from '@angular/core';
import { DatatableCellContext } from './datatable.types';

@Directive({
  selector: 'ng-template[appDatatableCell]',
})
export class DatatableCellTemplateDirective<T = unknown> {
  readonly columnId = input.required<string>({ alias: 'appDatatableCell' });
  readonly templateRef =
    inject<TemplateRef<DatatableCellContext<T>>>(TemplateRef);
}
