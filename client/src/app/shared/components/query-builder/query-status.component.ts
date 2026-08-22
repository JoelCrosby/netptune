import { Component, input } from '@angular/core';
import { FormErrorComponent } from '@static/components/form-error/form-error.component';

@Component({
  selector: 'app-query-status',
  imports: [FormErrorComponent],
  host: { class: 'block min-h-5' },
  template: `
    <div aria-live="polite">
      @if (messages().length) {
        @for (message of messages(); track message) {
          <app-form-error role="alert">{{ message }}</app-form-error>
        }
      } @else {
        <p class="text-foreground/45 text-[13px] leading-normal" role="status">
          <span
            class="text-foreground/30"
            i18n="Prefix of the plain-language query summary">
            Shows tasks where
          </span>
          {{ summary() }}
        </p>
      }
    </div>
  `,
})
export class QueryStatusComponent {
  readonly messages = input<string[]>([]);
  readonly summary = input('');
}
