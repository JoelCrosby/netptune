import { Component, computed, input, output } from '@angular/core';
import { LucideDynamicIcon, type LucideIconInput } from '@lucide/angular';
import { cn } from '../button/button.variants';
import { type Tone, toneClasses } from '../tones';

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
  readonly tone = input<Tone>('neutral');

  readonly activated = output();

  protected readonly tileClass = computed(() => {
    return cn(
      'flex h-9 w-9 items-center justify-center rounded-[9px]',
      toneClasses[this.tone()]
    );
  });
}
