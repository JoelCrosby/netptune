import { Component, computed, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AiQuestion, AiQuestionAnswer } from '@core/models/ai-conversation';
import {
  LucideArrowUp,
  LucideCheck,
  LucideCircleHelp,
  LucidePencil,
} from '@lucide/angular';

export interface AiQuestionResponse {
  question: AiQuestion;
  labels: string[];
  text: string | null;
}

@Component({
  selector: 'app-ai-assistant-question',
  host: { class: 'block' },
  imports: [
    FormsModule,
    LucideArrowUp,
    LucideCheck,
    LucideCircleHelp,
    LucidePencil,
  ],
  template: `
    <div
      class="border-border bg-card flex flex-col gap-3 rounded-2xl border p-4 pb-3"
      role="group"
      [attr.aria-label]="question().text">
      <div class="flex flex-col gap-1.5">
        @if (question().header; as header) {
          <span
            class="text-muted flex items-center gap-1.75 text-[11px] font-semibold tracking-[0.09em] uppercase">
            <svg
              lucideCircleHelp
              class="h-3.25 w-3.25 shrink-0"
              [class]="iconClass()"></svg>
            {{ header }}
          </span>
        }

        <p class="text-[15px] leading-snug font-semibold">
          {{ question().text }}
        </p>
      </div>

      <div class="flex flex-col gap-0.5">
        @for (option of question().options; track option.label) {
          <button
            type="button"
            class="flex w-full items-start gap-3 rounded-[10px] p-3 text-left transition-colors disabled:cursor-default"
            [class]="rowClass(option.label)"
            [disabled]="isSettled()"
            [attr.aria-pressed]="isChosen(option.label)"
            (click)="choose(option.label)">
            <span
              class="mt-px flex h-5 w-5 shrink-0 items-center justify-center rounded-md"
              [class]="badgeClass(option.label)">
              @if (isChosen(option.label)) {
                <svg
                  lucideCheck
                  class="text-primary-foreground h-3 w-3"
                  strokeWidth="3.2"></svg>
              } @else if (!question().multiSelect) {
                <span class="font-avatar text-[11px] leading-none">
                  {{ $index + 1 }}
                </span>
              }
            </span>

            <span class="flex min-w-0 flex-1 flex-col gap-0.75">
              <span class="text-[14.5px] leading-snug font-medium">
                {{ option.label }}
              </span>
              @if (option.description) {
                <span class="text-muted text-[13px] leading-normal">
                  {{ option.description }}
                </span>
              }
            </span>
          </button>
        }
      </div>

      @if (!isSettled()) {
        <div
          class="flex items-center gap-2.5 border-t pt-2.5"
          [class]="footerClass()">
          <svg lucidePencil class="text-muted h-3.25 w-3.25 shrink-0"></svg>

          <input
            type="text"
            class="placeholder:text-muted min-w-0 flex-1 bg-transparent py-1.5 text-[13.5px] outline-none"
            i18n-placeholder="
              Placeholder of the box for answering the assistant in your own
              words
            "
            placeholder="Type your answer"
            [ngModel]="typed()"
            (ngModelChange)="typed.set($event)"
            (keydown)="onKeydown($event)" />

          @if (showSendChoices()) {
            <button
              type="button"
              class="bg-primary text-primary-foreground hover:bg-primary/88 shrink-0 rounded-full px-3.5 py-1.5 text-xs font-semibold transition-colors"
              (click)="sendSelected()">
              <span i18n="Button that sends the options picked in an answer">
                Send
              </span>
            </button>
          } @else {
            <button
              type="button"
              class="flex h-7 w-7 shrink-0 items-center justify-center rounded-full transition-colors"
              [class]="sendClass()"
              [disabled]="!canSendTyped()"
              i18n-aria-label="
                Accessible label for the button that sends a typed answer
              "
              aria-label="Send answer"
              (click)="sendTyped()">
              <svg lucideArrowUp class="h-3.5 w-3.5" strokeWidth="2.4"></svg>
            </button>
          }
        </div>
      }
    </div>
  `,
})
export class AiAssistantQuestionComponent {
  readonly question = input.required<AiQuestion>();
  readonly answer = input<AiQuestionAnswer | null>(null);
  readonly isActive = input(false);

  readonly answered = output<AiQuestionResponse>();

  protected readonly selected = signal<string[]>([]);
  protected readonly typed = signal('');

  protected readonly isSettled = computed(() => {
    return this.answer() !== null || !this.isActive();
  });

  protected readonly canSendTyped = computed(() => {
    return this.typed().trim().length > 0;
  });

  protected readonly iconClass = computed(() => {
    return this.isSettled() ? 'text-muted' : 'text-primary';
  });

  // Typing wins over ticked options, so the one button in the footer follows
  // the answer the person is actually working on.
  protected readonly showSendChoices = computed(() => {
    const hasSelection = this.selected().length > 0;

    return this.question().multiSelect && hasSelection && !this.canSendTyped();
  });

  // The rule above the footer picks up the accent once focus has moved there.
  protected readonly footerClass = computed(() => {
    return this.canSendTyped() ? 'border-primary' : 'border-border/70';
  });

  protected readonly sendClass = computed(() => {
    if (this.canSendTyped()) {
      return 'bg-primary text-primary-foreground hover:bg-primary/88';
    }

    return 'bg-foreground/5 text-muted';
  });

  protected rowClass(label: string): string {
    if (this.isChosen(label)) {
      return 'bg-primary/10';
    }

    return this.isSettled() ? 'opacity-45' : 'hover:bg-hover';
  }

  protected badgeClass(label: string): string {
    if (this.isChosen(label)) {
      return 'bg-primary';
    }

    if (this.question().multiSelect) {
      return 'border-foreground/25 border';
    }

    return 'bg-foreground/8 text-muted';
  }

  protected isChosen(label: string): boolean {
    const answer = this.answer();

    if (answer) {
      return answer.selectedLabels.includes(label);
    }

    return this.selected().includes(label);
  }

  protected choose(label: string) {
    if (this.isSettled()) {
      return;
    }

    if (!this.question().multiSelect) {
      this.answered.emit({
        question: this.question(),
        labels: [label],
        text: null,
      });

      return;
    }

    this.selected.update((current) => {
      const isChosen = current.includes(label);

      if (isChosen) {
        return current.filter((chosen) => chosen !== label);
      }

      return [...current, label];
    });
  }

  protected sendSelected() {
    const labels = this.selected();

    if (labels.length === 0) {
      return;
    }

    this.answered.emit({ question: this.question(), labels, text: null });
  }

  protected sendTyped() {
    if (!this.canSendTyped()) {
      return;
    }

    this.answered.emit({
      question: this.question(),
      labels: [],
      text: this.typed().trim(),
    });
  }

  protected onKeydown(event: KeyboardEvent) {
    const isSubmit = event.key === 'Enter' && !event.shiftKey;

    if (!isSubmit) {
      return;
    }

    event.preventDefault();
    this.sendTyped();
  }
}
