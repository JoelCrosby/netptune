import { Component, computed, input, output } from '@angular/core';
import { LucideDynamicIcon, type LucideIconInput } from '@lucide/angular';
import { cn } from '../button/button.variants';

export type TemplateCardTone =
  'neutral' | 'blue' | 'purple' | 'amber' | 'green' | 'teal' | 'pink';

const TONES: Record<TemplateCardTone, string> = {
  neutral: 'bg-foreground/6 text-foreground/70',
  blue: 'bg-blue-500/10 text-blue-700 dark:text-blue-300',
  purple: 'bg-primary/10 text-primary',
  amber: 'bg-amber-500/12 text-amber-700 dark:text-amber-300',
  green: 'bg-emerald-500/12 text-emerald-700 dark:text-emerald-300',
  teal: 'bg-teal-500/12 text-teal-700 dark:text-teal-300',
  pink: 'bg-pink-500/12 text-pink-700 dark:text-pink-300',
};

@Component({
  selector: 'app-template-card',
  imports: [LucideDynamicIcon],
  host: { class: 'block' },
  template: `
    <button
      type="button"
      class="border-border bg-card hover:border-primary/40 focus-visible:ring-primary flex h-full w-full cursor-pointer flex-col items-start gap-3 rounded-xl border p-4 text-left transition-[border-color,box-shadow] hover:shadow-[0_4px_14px_rgba(0,0,0,0.06)] focus-visible:ring-2 focus-visible:outline-none"
      (click)="activated.emit()">
      <span [class]="tileClass()" aria-hidden="true">
        <svg [lucideIcon]="icon()" class="h-[18px] w-[18px]"></svg>
      </span>
      <span class="flex flex-col gap-[3px]">
        <span class="text-foreground text-[14.5px] font-semibold">{{
          heading()
        }}</span>
        @if (description()) {
          <span class="text-foreground/65 text-[13px] leading-snug">{{
            description()
          }}</span>
        }
      </span>
    </button>
  `,
})
export class TemplateCardComponent {
  readonly heading = input.required<string>();
  readonly description = input('');
  readonly icon = input.required<LucideIconInput>();
  readonly tone = input<TemplateCardTone>('neutral');

  readonly activated = output();

  protected readonly tileClass = computed(() => {
    return cn(
      'flex h-9 w-9 items-center justify-center rounded-[9px]',
      TONES[this.tone()]
    );
  });
}
