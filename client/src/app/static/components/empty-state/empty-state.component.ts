import { booleanAttribute, Component, input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  imports: [],
  host: { class: 'block' },
  template: `<div
    class="flex flex-col items-center justify-center gap-2 text-center"
    [class]="compact() ? 'min-h-32 py-4' : 'my-10 h-full'">
    <div class="text-muted" aria-hidden="true">
      <ng-content select="[emptyStateIcon]" />
    </div>

    <h4 [class]="compact() ? 'text-sm font-medium' : 'mx-8 font-normal'">
      {{ title() }}
    </h4>

    @if (description()) {
      <p
        class="text-sm"
        [class]="compact() ? 'text-foreground/60' : 'text-foreground/70 mb-2'">
        {{ description() }}
      </p>
    }

    <div class="mt-2 empty:hidden">
      <ng-content select="[emptyStateAction]" />
    </div>
  </div>`,
})
export class EmptyStateComponent {
  readonly title = input.required<string>();
  readonly description = input('');
  readonly compact = input(false, { transform: booleanAttribute });
}
