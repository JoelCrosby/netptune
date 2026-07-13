import { booleanAttribute, Component, input, output } from '@angular/core';

@Component({
  selector: 'app-selectable-card',
  host: { class: 'block' },
  template: `<div class="relative">
    <input
      class="peer absolute inset-0 z-10 m-0 h-full w-full cursor-pointer opacity-0 disabled:cursor-not-allowed"
      type="radio"
      [name]="groupName()"
      [checked]="selected()"
      [disabled]="disabled()"
      [attr.aria-label]="accessibleLabel()"
      (change)="selectionChange.emit()" />

    <div
      class="peer-focus-visible:ring-primary/50 flex w-full items-center gap-3 rounded-md border px-3 py-2.5 text-left transition-colors peer-focus-visible:ring-2 peer-focus-visible:ring-offset-2 peer-disabled:opacity-60"
      [class]="
        selected()
          ? 'border-primary bg-primary/5 text-foreground'
          : 'border-border text-foreground/80 hover:border-primary/40'
      ">
      <span
        class="flex h-4 w-4 flex-none items-center justify-center rounded-full border-2 transition-colors"
        [class]="selected() ? 'border-primary' : 'border-foreground/30'"
        aria-hidden="true">
        @if (selected()) {
          <span class="bg-primary h-2 w-2 rounded-full"></span>
        }
      </span>

      <div class="flex min-w-0 flex-1 items-center gap-3">
        <ng-content />
      </div>
    </div>
  </div>`,
})
export class SelectableCardComponent {
  readonly groupName = input.required<string>();
  readonly accessibleLabel = input.required<string>();
  readonly selected = input(false, { transform: booleanAttribute });
  readonly disabled = input(false, { transform: booleanAttribute });

  readonly selectionChange = output();
}
