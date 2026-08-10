import { Component, computed, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AiQuestion, AiQuestionAnswer } from '@core/models/ai-conversation';
import { LucideCheck, LucidePencil, LucideSend } from '@lucide/angular';

export interface AiQuestionResponse {
  question: AiQuestion;
  labels: string[];
  text: string | null;
}

@Component({
  selector: 'app-ai-assistant-question',
  host: { class: 'block' },
  imports: [FormsModule, LucideCheck, LucidePencil, LucideSend],
  template: `
    <div
      class="border-border bg-hover rounded-2xl border p-3"
      role="group"
      [attr.aria-label]="question().text">
      @if (question().header; as header) {
        <span
          class="bg-hover text-muted rounded px-1.5 py-0.5 text-[0.65rem] tracking-wide uppercase">
          {{ header }}
        </span>
      }

      <p class="mt-1.5 text-sm font-medium">{{ question().text }}</p>

      <div class="mt-2.5 flex flex-col gap-1.5">
        @for (option of question().options; track option.label) {
          <button
            type="button"
            class="border-border/60 hover:border-primary/60 flex w-full flex-col gap-0.5 rounded-xl border px-3 py-2 text-left transition-colors disabled:cursor-default"
            [class]="optionClass(option.label)"
            [disabled]="isSettled()"
            [attr.aria-pressed]="isChosen(option.label)"
            (click)="choose(option.label)">
            <span class="flex items-center gap-1.5 text-sm">
              @if (isChosen(option.label)) {
                <svg
                  lucideCheck
                  class="text-primary h-3.5 w-3.5 shrink-0"></svg>
              }
              {{ option.label }}
            </span>
            @if (option.description) {
              <span class="text-muted text-xs">{{ option.description }}</span>
            }
          </button>
        }
      </div>

      @if (!isSettled()) {
        @if (isTyping()) {
          <div class="mt-2 flex items-end gap-2">
            <textarea
              rows="2"
              class="border-border placeholder:text-muted flex-1 resize-none rounded-xl border bg-transparent px-3 py-2 text-sm outline-none"
              i18n-placeholder="
                Placeholder of the box for answering the assistant in your own
                words
              "
              placeholder="Type your answer"
              [ngModel]="typed()"
              (ngModelChange)="typed.set($event)"
              (keydown)="onKeydown($event)"></textarea>

            <button
              type="button"
              class="bg-primary text-primary-foreground flex h-9 w-9 shrink-0 items-center justify-center rounded-full transition disabled:opacity-40"
              [disabled]="!canSendTyped()"
              i18n-aria-label="
                Accessible label for the button that sends a typed answer
              "
              aria-label="Send answer"
              (click)="sendTyped()">
              <svg lucideSend class="h-4 w-4"></svg>
            </button>
          </div>
        } @else {
          <div class="mt-2 flex items-center justify-between gap-2">
            <button
              type="button"
              class="text-muted hover:text-foreground flex items-center gap-1 text-xs"
              (click)="startTyping()">
              <svg lucidePencil class="h-3 w-3"></svg>
              <span
                i18n="
                  Button that opens a box for answering the assistant in your
                  own words
                ">
                Something else
              </span>
            </button>

            @if (question().multiSelect) {
              <button
                type="button"
                class="bg-primary text-primary-foreground rounded-full px-3 py-1.5 text-xs transition disabled:opacity-40"
                [disabled]="selected().length === 0"
                (click)="sendSelected()">
                <span i18n="Button that sends the options picked in an answer">
                  Send
                </span>
              </button>
            }
          </div>
        }
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
  protected readonly isTyping = signal(false);
  protected readonly typed = signal('');

  protected readonly isSettled = computed(() => {
    return this.answer() !== null || !this.isActive();
  });

  protected readonly canSendTyped = computed(() => {
    return this.typed().trim().length > 0;
  });

  protected optionClass(label: string): string {
    const isChosen = this.isChosen(label);

    if (isChosen) {
      return 'border-primary bg-primary/10';
    }

    return this.isSettled() ? 'opacity-60' : '';
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

  protected startTyping() {
    this.isTyping.set(true);
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
    if (event.key === 'Escape') {
      this.isTyping.set(false);

      return;
    }

    const isSubmit = event.key === 'Enter' && !event.shiftKey;

    if (!isSubmit) {
      return;
    }

    event.preventDefault();
    this.sendTyped();
  }
}
