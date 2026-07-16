import { booleanAttribute, Component, input, output } from '@angular/core';
import { LucideCheck } from '@lucide/angular';

export type SelectableCardVariant = 'default' | 'feature';

@Component({
  selector: 'app-selectable-card',
  imports: [LucideCheck],
  host: { class: 'block' },
  template: `
    <div class="relative h-full">
      <input
        class="peer absolute inset-0 z-10 m-0 h-full w-full cursor-pointer opacity-0 disabled:cursor-not-allowed"
        type="radio"
        [name]="groupName()"
        [checked]="selected()"
        [disabled]="disabled()"
        [attr.aria-label]="accessibleLabel()"
        (change)="selectionChange.emit()" />

      @if (variant() === 'feature') {
        <div
          class="border-border from-primary/5 to-card peer-focus-visible:ring-primary/50 group hover:border-primary/50 relative h-full w-full overflow-hidden rounded-xl border bg-linear-to-b p-4 text-left transition-all peer-focus-visible:ring-2 peer-focus-visible:ring-offset-2 peer-disabled:opacity-60"
          [class]="
            selected() ? 'border-primary ring-primary/20 shadow-xl ring-1' : ''
          ">
          <span
            class="bg-primary/0 group-hover:bg-primary/20 pointer-events-none absolute -top-12 -right-12 h-28 w-28 rounded-full blur-2xl transition-colors"
            [class.bg-primary/20]="selected()"
            aria-hidden="true"></span>

          <div class="relative flex items-start justify-between gap-3">
            <span
              class="text-primary from-primary/20 to-primary/5 ring-primary/10 relative flex h-10 w-10 items-center justify-center rounded-xl bg-linear-to-br ring-1 ring-inset">
              <ng-content select="[selectableCardIcon]" />
            </span>

            <span class="relative flex items-center gap-2">
              @if (badge()) {
                <span
                  class="bg-primary/10 text-primary rounded-full px-2 py-1 text-xs font-semibold tracking-wide uppercase">
                  {{ badge() }}
                </span>
              }
              <span
                class="selection-mark border-border flex h-6 w-6 shrink-0 items-center justify-center rounded-full border"
                [class.border-primary]="selected()"
                [class.bg-primary]="selected()"
                [class.text-background]="selected()"
                aria-hidden="true">
                @if (selected()) {
                  <svg lucideCheck class="h-3.5 w-3.5"></svg>
                }
              </span>
            </span>
          </div>

          <span class="relative mt-4 block text-base font-semibold">
            {{ heading() }}
          </span>
          @if (description()) {
            <span
              class="text-muted relative mt-1 line-clamp-2 text-xs leading-5">
              {{ description() }}
            </span>
          }
        </div>
      } @else {
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
      }
    </div>
  `,
})
export class SelectableCardComponent {
  readonly groupName = input.required<string>();
  readonly accessibleLabel = input.required<string>();
  readonly selected = input(false, { transform: booleanAttribute });
  readonly disabled = input(false, { transform: booleanAttribute });
  readonly variant = input<SelectableCardVariant>('default');
  readonly heading = input('');
  readonly description = input('');
  readonly badge = input('');

  readonly selectionChange = output();
}
