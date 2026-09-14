import { Directive, computed, input } from '@angular/core';
import { cn } from '../components/button/button.variants';

// `eyebrow` is the smaller, wider-tracked label the task dialogs put above a field.
export type SectionLabelVariant = 'default' | 'eyebrow';

const variantClasses: Record<SectionLabelVariant, string> = {
  default: 'text-muted text-xs font-semibold tracking-wide uppercase',
  eyebrow: 'font-avatar text-muted text-[10px] tracking-[0.14em] uppercase',
};

// The small uppercase label that heads a group of fields or rows. A directive rather
// than a component so the call site keeps whatever element the outline needs.
@Directive({
  selector: '[appSectionLabel]',
  host: { '[class]': 'hostClass()' },
})
export class SectionLabelDirective {
  readonly variant = input<SectionLabelVariant>('default');
  readonly class = input('');

  protected readonly hostClass = computed(() => {
    return cn(variantClasses[this.variant()], this.class());
  });
}
