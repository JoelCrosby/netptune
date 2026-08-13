import { Component, input, output } from '@angular/core';

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
      class="flex h-6 flex-row items-center gap-1 rounded-full border px-2 text-xs transition-colors"
      [class]="
        reaction().reacted
          ? 'border-primary bg-primary/10 text-primary'
          : 'border-neutral-200 hover:border-neutral-300 dark:border-neutral-700 dark:hover:border-neutral-600'
      "
      [disabled]="disabled()"
      [attr.aria-pressed]="reaction().reacted"
      [attr.aria-label]="reaction().value"
      (click)="reactionToggle.emit(reaction().value)">
      <span aria-hidden="true">{{ reaction().value }}</span>
      <span class="font-medium">{{ reaction().count }}</span>
    </button>
  `,
})
export class ReactionButtonComponent {
  readonly reaction = input.required<ReactionGroup>();
  readonly disabled = input<boolean>(false);

  readonly reactionToggle = output<string>();
}
