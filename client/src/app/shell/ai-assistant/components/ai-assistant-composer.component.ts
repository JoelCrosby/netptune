import { Component, computed, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AiEffort, AiEffortOption } from '@core/models/ai-effort';
import { AiModelOption } from '@core/models/ai-model';
import { LucideArrowUp, LucideSquare } from '@lucide/angular';
import { AiAssistantEffortMenuComponent } from './ai-assistant-effort-menu.component';
import { AiAssistantModelMenuComponent } from './ai-assistant-model-menu.component';

@Component({
  selector: 'app-ai-assistant-composer',
  host: { class: 'block p-3' },
  imports: [
    FormsModule,
    LucideArrowUp,
    LucideSquare,
    AiAssistantModelMenuComponent,
    AiAssistantEffortMenuComponent,
  ],
  template: `
    <div class="mx-auto w-full" [class]="contentWidth()">
      <div
        class="bg-hover focus-within:ring-primary/40 rounded-2xl p-2 transition focus-within:ring-2">
        <textarea
          rows="3"
          class="placeholder:text-muted w-full resize-none bg-transparent px-2 py-1.5 text-sm outline-none"
          [ngModel]="draft()"
          (ngModelChange)="draftChanged.emit($event)"
          (keydown)="onKeydown($event)"
          [placeholder]="placeholder()"
          [disabled]="disabled() || isApplying()"></textarea>

        @if (isReplacing()) {
          <div
            class="text-muted flex items-center justify-between gap-2 px-2 pb-1 text-xs">
            <span i18n="Shown while a question is being reworded">
              Editing your last question — sending replaces the reply.
            </span>
            <button
              type="button"
              class="hover:text-foreground shrink-0"
              (click)="editCancelled.emit()">
              <span i18n="Dismisses a dialog without acting">Cancel</span>
            </button>
          </div>
        }

        <div class="flex items-center justify-between gap-2 pt-1">
          @if (models().length > 0) {
            <div class="flex min-w-0 items-center gap-2">
              <app-ai-assistant-model-menu
                [models]="models()"
                [selectedModel]="selectedModel()"
                [label]="modelLabel()"
                (selected)="modelSelected.emit($event)" />

              @if (supportsEffort()) {
                <app-ai-assistant-effort-menu
                  [efforts]="efforts()"
                  [selectedEffort]="selectedEffort()"
                  [label]="effortLabel()"
                  (selected)="effortSelected.emit($event)" />
              }
            </div>
          } @else {
            <span></span>
          }

          @if (isStreaming()) {
            <button
              type="button"
              class="bg-foreground/15 text-foreground hover:bg-foreground/25 flex h-9 w-9 items-center justify-center rounded-full transition"
              i18n-aria-label="
                Accessible label for the button that stops the assistant
              "
              aria-label="Stop the assistant"
              (click)="stopped.emit()">
              <svg lucideSquare class="h-3.5 w-3.5 fill-current"></svg>
            </button>
          } @else {
            <button
              type="button"
              class="bg-primary text-primary-foreground flex h-9 w-9 items-center justify-center rounded-full transition disabled:opacity-40"
              [disabled]="!canSend()"
              i18n-aria-label="
                Accessible label for the button that sends a message
              "
              aria-label="Send message"
              (click)="send()">
              <svg lucideArrowUp class="h-4 w-4"></svg>
            </button>
          }
        </div>
      </div>
    </div>
  `,
})
export class AiAssistantComposerComponent {
  readonly models = input.required<AiModelOption[]>();
  readonly selectedModel = input.required<string | null>();
  readonly modelLabel = input.required<string>();
  readonly efforts = input.required<AiEffortOption[]>();
  readonly selectedEffort = input.required<AiEffort | null>();
  readonly effortLabel = input.required<string>();
  readonly supportsEffort = input(false);
  readonly isStreaming = input(false);
  readonly isApplying = input(false);
  readonly isReplacing = input(false);
  readonly isAnswering = input(false);
  readonly disabled = input(false);
  readonly contentWidth = input('');
  readonly draft = input('');

  readonly messageSent = output<string>();
  readonly modelSelected = output<string | null>();
  readonly effortSelected = output<AiEffort | null>();
  readonly draftChanged = output<string>();
  readonly stopped = output();
  readonly editCancelled = output();

  protected readonly canSend = computed(() => {
    const hasDraft = this.draft().trim().length > 0;
    const isBusy = this.isStreaming() || this.isApplying();

    return hasDraft && !isBusy && !this.disabled();
  });

  protected placeholder(): string {
    if (this.isStreaming()) {
      return $localize`:Placeholder while the assistant is replying:Waiting for the assistant…`;
    }

    if (this.isApplying()) {
      return $localize`:Placeholder while proposed changes are being applied:Waiting for the current changes to finish…`;
    }

    if (this.isAnswering()) {
      return $localize`:Placeholder while the assistant is waiting on an answer:Pick an option above, or answer here`;
    }

    return $localize`:Placeholder for the assistant message input:Ask about your workspace`;
  }

  protected onKeydown(event: KeyboardEvent) {
    const isSubmit = event.key === 'Enter' && !event.shiftKey;

    if (!isSubmit) {
      return;
    }

    event.preventDefault();
    this.send();
  }

  protected send() {
    if (!this.canSend()) {
      return;
    }

    this.messageSent.emit(this.draft());
  }
}
