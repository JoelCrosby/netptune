import { Component, input, output } from '@angular/core';
import { LucideX } from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';

@Component({
  selector: 'app-bulk-edit-row',
  imports: [IconButtonComponent, LucideX],
  host: { class: 'flex items-start gap-3' },
  template: `
    <label
      class="text-muted w-26 shrink-0 pt-3 text-[15px] font-medium"
      [attr.for]="controlId()">
      {{ label() }}
    </label>

    <div class="min-w-0 flex-1">
      <ng-content />

      @if (hint(); as hint) {
        <div class="text-muted mx-[3px] mt-1.5 text-xs font-medium">
          {{ hint }}
        </div>
      }
    </div>

    <button
      type="button"
      app-icon-button
      class="text-muted hover:text-foreground mt-1.5 h-8 w-8 shrink-0"
      [attr.aria-label]="removeLabel()"
      (click)="removed.emit()">
      <svg lucideX class="h-4 w-4" aria-hidden="true"></svg>
    </button>
  `,
})
export class BulkEditRowComponent {
  readonly label = input.required<string>();
  readonly controlId = input<string>();
  readonly hint = input<string>();
  readonly removeLabel = input.required<string>();

  readonly removed = output();
}
