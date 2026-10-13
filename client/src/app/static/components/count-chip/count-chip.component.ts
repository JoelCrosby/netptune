import { Component, computed, input } from '@angular/core';
import { cn } from '../button/button.variants';
import { SignalTone, signalToneDot } from '../tones';

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'button[app-count-chip]',
  host: {
    type: 'button',
    '[class]': 'hostClass()',
    '[attr.aria-pressed]': 'active()',
  },
  template: `
    <span class="h-1.75 w-1.75 rounded-full" [class]="dotClass()"></span>
    <span class="font-bold">{{ count() }}</span>
    <span class="text-foreground/70"><ng-content /></span>
  `,
})
export class CountChipComponent {
  readonly count = input.required<number>();
  readonly tone = input<SignalTone>('muted');
  readonly active = input(false);

  protected readonly dotClass = computed(() => signalToneDot[this.tone()]);

  protected readonly hostClass = computed(() => {
    return cn(
      'focus-visible:ring-primary flex h-8 cursor-pointer items-center gap-2 rounded-lg border px-3 text-[13px] transition-colors outline-none focus-visible:ring-2',
      this.active()
        ? 'border-primary bg-primary/10'
        : 'border-border bg-card hover:border-foreground/40'
    );
  });
}
