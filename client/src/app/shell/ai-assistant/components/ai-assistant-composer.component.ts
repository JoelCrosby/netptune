import {
  Component,
  ElementRef,
  computed,
  input,
  output,
  viewChild,
} from '@angular/core';
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
  host: { class: 'block' },
  imports: [
    AiAssistantContextComponent,
    AiAssistantEffortMenuComponent,
    AiAssistantModelMenuComponent,
    FormsModule,
    LucideArrowUp,
    LucideSquare,
  ],
  template: `
    <div class="mx-auto w-full" [class]="outerClass()">
      <div
        class="bg-form-field-background border-foreground/15 focus-within:border-primary flex flex-col gap-1 border transition-colors"
        [class]="fieldClass()">
        @if (hasContextRow()) {
          <app-ai-assistant-context
            class="px-0.5 pt-0.5"
            [chips]="chips()"
            [hasRemoved]="hasRemovedContext()"
            (removed)="contextRemoved.emit($event)"
            (restored)="contextRestored.emit()" />
        }

        <textarea
          #input
          [rows]="roomy() ? 3 : 2"
          class="placeholder:text-muted w-full resize-none bg-transparent outline-none"
          [class]="textareaClass()"
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
              class="bg-foreground/5 text-foreground ring-foreground/15 hover:bg-foreground/10 flex shrink-0 items-center justify-center rounded-full ring-1 transition-colors ring-inset"
              [class]="roomy() ? 'h-9.5 w-9.5' : 'h-8 w-8'"
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
              class="flex shrink-0 items-center justify-center rounded-full transition-colors"
              [class]="sendClass()"
              [disabled]="!canSend()"
              i18n-aria-label="
                Accessible label for the button that sends a message
              "
              aria-label="Send message"
              (click)="send()">
              <svg
                lucideArrowUp
                [class]="roomy() ? 'h-4.5 w-4.5' : 'h-3.75 w-3.75'"
                strokeWidth="2.2"></svg>
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
  readonly roomy = input(false);
  // Centred under the greeting on a new chat, rather than docked at the bottom.
  readonly inline = input(false);

  readonly messageSent = output<string>();
  readonly modelSelected = output<string | null>();
  readonly effortSelected = output<AiEffort | null>();
  readonly draftChanged = output<string>();
  readonly stopped = output();
  readonly editCancelled = output();
  readonly contextRemoved = output<AiContextChip>();
  readonly contextRestored = output();

  private readonly input =
    viewChild.required<ElementRef<HTMLTextAreaElement>>('input');

  protected readonly hasContextRow = computed(() => {
    return this.chips().length > 0 || this.hasRemovedContext();
  });

  protected readonly outerClass = computed(() => {
    const width = this.contentWidth();

    if (this.inline()) {
      return width;
    }

    return this.roomy() ? `${width} px-6 pb-6` : `${width} px-4.5 pb-3.5`;
  });

  protected readonly fieldClass = computed(() => {
    if (this.roomy()) {
      return 'rounded-[20px] p-2.5 shadow-sm';
    }

    return 'rounded-[14px] p-2';
  });

  protected readonly textareaClass = computed(() => {
    if (this.roomy()) {
      return 'px-2 pt-2 pb-1 text-[15px] leading-relaxed';
    }

    return 'px-1.5 pt-1.5 pb-0.5 text-[13.5px] leading-normal';
  });

  protected readonly sendClass = computed(() => {
    const size = this.roomy() ? 'h-9.5 w-9.5' : 'h-8 w-8';

    if (this.canSend()) {
      return `${size} bg-primary text-primary-foreground hover:bg-primary/88`;
    }

    return `${size} bg-foreground/5 text-muted`;
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

  // Puts the caret at the end so a seeded draft can be finished straight away.
  // ngModel writes the new draft a tick later, so the caret waits for it.
  focus() {
    const textarea = this.input().nativeElement;

    textarea.focus();

    setTimeout(() => {
      const end = textarea.value.length;

      textarea.setSelectionRange(end, end);
    });
  }

  protected send() {
    if (!this.canSend()) {
      return;
    }

    this.messageSent.emit(this.draft());
  }
}
