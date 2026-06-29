import { Component } from '@angular/core';

@Component({
  selector: 'app-form-error',
  template: `
    <div class="text-warn text-sm font-medium tracking-[.0125px]">
      <ng-content />
    </div>
  `,
})
export class FormErrorComponent {}
