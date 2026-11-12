import { Component, computed, input, output } from '@angular/core';

export interface ReactionGroup {
  value: string;
  count: number;
  reacted: boolean;
}

@Component({
  selector: 'app-reaction-button',
  host: { class: 'inline-flex' },
  template: `
    <button
      type="button"
      class="focus-visible:ring-primary flex h-6 flex-row items-center gap-1 rounded-full border px-2 text-xs transition-colors focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none"
      [class]="
        reaction().reacted
          ? 'border-primary bg-primary/10 text-primary'
          : 'border-neutral-200 hover:border-neutral-300 dark:border-neutral-700 dark:hover:border-neutral-600'
      "
      [disabled]="disabled()"
      [attr.aria-pressed]="reaction().reacted"
      [attr.aria-label]="accessibleName()"
      (click)="reactionToggle.emit(reaction().value)">
      <span aria-hidden="true">{{ reaction().value }}</span>
      <span class="font-medium" aria-hidden="true">{{ reaction().count }}</span>
    </button>
  `,
})
export class ReactionButtonComponent {
  readonly reaction = input.required<ReactionGroup>();
  readonly disabled = input<boolean>(false);

  readonly reactionToggle = output<string>();

  protected readonly accessibleName = computed(() => {
    const { value, count } = this.reaction();

    return $localize`:Accessible name of a reaction toggle. EMOJI is the reaction, COUNT how many people reacted:${value}:EMOJI: reaction, ${count}:COUNT:`;
  });
}
