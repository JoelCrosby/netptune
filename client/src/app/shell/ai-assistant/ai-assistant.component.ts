import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { TemplatePortal } from '@angular/cdk/portal';
import {
  AfterViewInit,
  Component,
  HostListener,
  OnDestroy,
  TemplateRef,
  ViewContainerRef,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AiChangeSetStatus } from '@core/models/ai-conversation';
import { AiAssistantService } from '@core/services/ai-assistant.service';
import {
  LucideHistory,
  LucideSparkles,
  LucideTrash,
  LucideWrench,
  LucideX,
} from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { TooltipDirective } from '@static/directives/tooltip.directive';

@Component({
  selector: 'app-ai-assistant',
  imports: [
    FormsModule,
    LucideHistory,
    LucideSparkles,
    LucideTrash,
    LucideWrench,
    LucideX,
    FlatButtonComponent,
    IconButtonComponent,
    StrokedButtonComponent,
    TooltipDirective,
  ],
  template: `
    <ng-template #panelTmpl>
      <div
        class="bg-board-group text-popover-foreground border-border flex h-full w-full flex-col border-l shadow-lg"
        role="dialog"
        aria-modal="false"
        i18n-aria-label="Accessible name of the AI assistant panel"
        aria-label="AI assistant">
        <header
          class="border-border flex items-center justify-between gap-2 border-b px-4 py-3">
          <div class="flex items-center gap-2">
            <svg lucideSparkles class="text-primary h-4 w-4"></svg>
            <h2
              class="font-overpass text-[1.05rem] font-normal"
              i18n="Title of the AI assistant panel">
              Assistant
            </h2>
          </div>

          <div class="flex items-center gap-1">
            <button
              app-icon-button
              type="button"
              appTooltip
              i18n-appTooltip="
                Tooltip on the button that lists past conversations
              "
              appTooltip="Conversation history"
              (click)="toggleHistory()">
              <svg lucideHistory class="h-4 w-4"></svg>
            </button>
            <button
              app-stroked-button
              type="button"
              class="h-8 px-2 text-xs"
              (click)="startNew()">
              <span i18n="Button that clears the assistant conversation"
                >New chat</span
              >
            </button>
            <button app-icon-button type="button" (click)="assistant.close()">
              <svg lucideX class="h-4 w-4"></svg>
            </button>
          </div>
        </header>

        @if (assistant.showHistory()) {
          <div class="flex-1 overflow-y-auto px-4 py-4">
            @for (
              conversation of assistant.conversations();
              track conversation.id
            ) {
              <div class="border-border flex items-center gap-2 border-b py-2">
                <button
                  type="button"
                  class="min-w-0 flex-1 text-left text-sm hover:underline"
                  (click)="openConversation(conversation.id)">
                  <span class="block truncate">{{ conversation.title }}</span>
                  <span class="text-muted text-xs">
                    {{ conversation.messageCount }}
                    <span i18n="Counts messages in a stored conversation"
                      >messages</span
                    >
                  </span>
                </button>
                <button
                  app-icon-button
                  type="button"
                  (click)="deleteConversation(conversation.id)">
                  <svg lucideTrash class="h-4 w-4"></svg>
                </button>
              </div>
            } @empty {
              <p
                class="text-muted text-sm"
                i18n="Empty state for stored conversations">
                There are no earlier conversations.
              </p>
            }
          </div>
        } @else {
          <div class="flex-1 overflow-y-auto px-4 py-4">
            @if (entries().length === 0) {
              <p
                class="text-muted text-sm"
                i18n="Empty state inside the AI assistant panel">
                Ask about your workspace — projects, tasks, statuses. Any change
                the assistant suggests is shown here for you to review before it
                is applied.
              </p>
            }

            <div class="flex flex-col gap-4">
              @for (entry of entries(); track $index) {
                <div class="flex flex-col gap-1">
                  <span class="text-muted text-xs">
                    @if (entry.role === 'user') {
                      <span i18n="Label for a message the user sent">You</span>
                    } @else {
                      <span i18n="Label for a message the assistant sent"
                        >Assistant</span
                      >
                    }
                  </span>

                  @if (entry.tools.length > 0) {
                    <div
                      class="text-muted flex flex-wrap items-center gap-2 text-xs">
                      <svg lucideWrench class="h-3 w-3"></svg>
                      @for (tool of entry.tools; track $index) {
                        <span class="font-mono">{{ tool }}</span>
                      }
                    </div>
                  }

                  <p
                    class="text-sm whitespace-pre-wrap"
                    [class.text-error]="entry.failed">
                    {{ entry.text }}
                  </p>
                </div>
              }
            </div>
          </div>
        }

        @if (changeSet(); as proposal) {
          <section class="border-border bg-card border-t px-4 py-3">
            <h3
              class="font-overpass mb-2 text-sm font-normal"
              i18n="Heading above the list of proposed workspace changes">
              Proposed changes
            </h3>

            <div class="flex flex-col gap-2">
              @for (change of proposal.changes; track change.id) {
                <label class="flex items-start gap-2 text-sm">
                  <input
                    type="checkbox"
                    class="mt-1"
                    [checked]="isIncluded(change.id)"
                    [disabled]="!isPending()"
                    (change)="assistant.toggleChange(change.id)" />
                  <span class="flex flex-col gap-0.5">
                    <span>{{ change.summary }}</span>
                    @for (field of change.fields; track field.name) {
                      <span class="text-muted text-xs">
                        {{ field.name }}:
                        @if (field.before) {
                          <span class="line-through">{{ field.before }}</span>
                        }
                        <span>{{ field.after }}</span>
                      </span>
                    }
                    @if (change.applyError) {
                      <span class="text-error text-xs">{{
                        change.applyError
                      }}</span>
                    }
                  </span>
                </label>
              }
            </div>

            @if (isPending()) {
              <div class="mt-3 flex items-center gap-2">
                <button
                  app-flat-button
                  type="button"
                  [disabled]="assistant.isApplying()"
                  (click)="apply()">
                  <span i18n="Button that applies the proposed changes"
                    >Apply</span
                  >
                </button>
                <button app-stroked-button type="button" (click)="discard()">
                  <span i18n="Button that discards the proposed changes"
                    >Discard</span
                  >
                </button>
              </div>
            } @else {
              <p
                class="text-muted mt-3 text-xs"
                i18n="Shown after changes were applied">
                These changes have been applied.
              </p>
            }
          </section>
        }

        <footer class="border-border border-t p-3">
          <div class="flex items-end gap-2">
            <textarea
              rows="2"
              class="border-border bg-background placeholder:text-muted w-full resize-none rounded border px-3 py-2 text-sm outline-none"
              [ngModel]="draft()"
              (ngModelChange)="draft.set($event)"
              (keydown)="onKeydown($event)"
              [placeholder]="inputPlaceholder()"
              [disabled]="assistant.isStreaming()"></textarea>
            <button
              app-stroked-button
              type="button"
              [disabled]="!canSend()"
              (click)="send()">
              <span i18n="Button that sends a message to the assistant"
                >Send</span
              >
            </button>
          </div>
        </footer>
      </div>
    </ng-template>
  `,
})
export class AiAssistantComponent implements AfterViewInit, OnDestroy {
  private readonly panelTmpl =
    viewChild.required<TemplateRef<unknown>>('panelTmpl');

  private readonly overlay = inject(Overlay);
  private readonly vcr = inject(ViewContainerRef);
  private readonly overlayRef: OverlayRef;
  private portal!: TemplatePortal;

  protected readonly assistant = inject(AiAssistantService);
  protected readonly draft = signal('');
  protected readonly entries = computed(() => this.assistant.entries());

  protected readonly canSend = computed(() => {
    const hasDraft = this.draft().trim().length > 0;

    return hasDraft && !this.assistant.isStreaming();
  });

  protected readonly changeSet = computed(() => this.assistant.changeSet());

  protected readonly isPending = computed(() => {
    return this.changeSet()?.status === AiChangeSetStatus.pending;
  });

  protected isIncluded(changeId: number): boolean {
    return !this.assistant.excludedChangeIds().has(changeId);
  }

  protected apply() {
    void this.assistant.applyChangeSet();
  }

  protected discard() {
    void this.assistant.discardChangeSet();
  }

  constructor() {
    this.overlayRef = this.overlay.create({
      hasBackdrop: false,
      positionStrategy: this.overlay.position().global().right().top(),
      scrollStrategy: this.overlay.scrollStrategies.noop(),
      width: 'min(100vw, 26rem)',
      height: '100%',
    });

    effect(() => {
      const isOpen = this.assistant.isOpen();

      if (isOpen && this.portal && !this.overlayRef.hasAttached()) {
        this.overlayRef.attach(this.portal);

        return;
      }

      if (!isOpen && this.overlayRef.hasAttached()) {
        this.overlayRef.detach();
      }
    });
  }

  ngAfterViewInit() {
    this.portal = new TemplatePortal(this.panelTmpl(), this.vcr);
  }

  ngOnDestroy() {
    this.overlayRef.dispose();
  }

  @HostListener('document:keydown', ['$event'])
  onDocumentKeydown(event: KeyboardEvent) {
    const isToggle = (event.ctrlKey || event.metaKey) && event.key === 'i';

    if (isToggle) {
      event.preventDefault();
      this.assistant.toggle();
    }
  }

  protected inputPlaceholder(): string {
    if (this.assistant.isStreaming()) {
      return $localize`:Placeholder while the assistant is replying:Waiting for the assistant…`;
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

  protected toggleHistory() {
    void this.assistant.toggleHistory();
  }

  protected openConversation(conversationId: string) {
    void this.assistant.openConversation(conversationId);
  }

  protected deleteConversation(conversationId: string) {
    void this.assistant.deleteConversation(conversationId);
  }

  protected startNew() {
    this.assistant.startNewConversation();
    this.draft.set('');
  }

  protected send() {
    if (!this.canSend()) {
      return;
    }

    const text = this.draft();

    this.draft.set('');

    void this.assistant.send(text);
  }
}
