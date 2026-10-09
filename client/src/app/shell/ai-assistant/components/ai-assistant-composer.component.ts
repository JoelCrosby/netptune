import { Component, computed, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AiContextChip } from '@core/models/ai-context';
import { AiEffort, AiEffortOption } from '@core/models/ai-effort';
import { AiModelOption } from '@core/models/ai-model';
import { LucideArrowUp, LucideSquare } from '@lucide/angular';
import { AiAssistantContextComponent } from './ai-assistant-context.component';
import { AiAssistantEffortMenuComponent } from './ai-assistant-effort-menu.component';
import { AiAssistantModelMenuComponent } from './ai-assistant-model-menu.component';

@Component({
  selector: 'app-ai-assistant-composer',
  host: { class: 'block px-4 pb-3.5' },
  imports: [
    AiAssistantContextComponent,
    AiAssistantEffortMenuComponent,
    AiAssistantModelMenuComponent,
    FormsModule,
    LucideArrowUp,
    LucideSquare,
  ],
  template: `
    <div class="mx-auto w-full" [class]="contentWidth()">
      <div
        class="bg-form-field-background border-foreground/15 focus-within:border-primary flex flex-col gap-1 rounded-[14px] border p-2 transition-colors">
        @if (hasContextRow()) {
          <app-ai-assistant-context
            class="px-0.5 pt-0.5"
            [chips]="chips()"
            [hasRemoved]="hasRemovedContext()"
            (removed)="contextRemoved.emit($event)"
            (restored)="contextRestored.emit()" />
        }

        <textarea
          rows="2"
          class="placeholder:text-muted w-full resize-none bg-transparent px-1.5 pt-1.5 pb-0.5 text-[13.5px] leading-normal outline-none"
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

        <div class="flex items-center justify-between gap-2">
          @if (models().length > 0) {
            <div class="flex min-w-0 items-center gap-1">
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
            <span
              class="text-muted ml-auto text-[11px] whitespace-nowrap"
              i18n="
                Hint beside the stop button saying the key that stops a reply
              ">
              Esc to stop
            </span>
            <button
              type="button"
              class="bg-foreground/5 text-foreground ring-foreground/15 hover:bg-foreground/10 flex h-8 w-8 shrink-0 items-center justify-center rounded-full ring-1 transition-colors ring-inset"
              i18n-aria-label="
                Accessible label for the button that stops the assistant
              "
              aria-label="Stop the assistant"
              (click)="stopped.emit()">
              <svg lucideSquare class="h-3 w-3 fill-current"></svg>
            </button>
          } @else {
            <button
              type="button"
              class="flex h-8 w-8 shrink-0 items-center justify-center rounded-full transition-colors"
              [class]="
                canSend()
                  ? 'bg-primary text-primary-foreground hover:bg-primary/88'
                  : 'bg-foreground/5 text-muted'
              "
              [disabled]="!canSend()"
              i18n-aria-label="
                Accessible label for the button that sends a message
              "
              aria-label="Send message"
              (click)="send()">
              <svg lucideArrowUp class="h-3.75 w-3.75" strokeWidth="2.2"></svg>
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
  readonly chips = input<readonly AiContextChip[]>([]);
  readonly hasRemovedContext = input(false);

  readonly messageSent = output<string>();
  readonly modelSelected = output<string | null>();
  readonly effortSelected = output<AiEffort | null>();
  readonly draftChanged = output<string>();
  readonly stopped = output();
  readonly editCancelled = output();
  readonly contextRemoved = output<AiContextChip>();
  readonly contextRestored = output();

  protected readonly hasContextRow = computed(() => {
    return this.chips().length > 0 || this.hasRemovedContext();
  });

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
