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
import { AiAssistantService } from '@core/services/ai-assistant.service';
import { LucideSparkles, LucideWrench, LucideX } from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';

@Component({
  selector: 'app-ai-assistant',
  imports: [
    FormsModule,
    LucideSparkles,
    LucideWrench,
    LucideX,
    IconButtonComponent,
    StrokedButtonComponent,
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

        <div class="flex-1 overflow-y-auto px-4 py-4">
          @if (entries().length === 0) {
            <p
              class="text-muted text-sm"
              i18n="Empty state inside the AI assistant panel">
              Ask about your workspace — projects, tasks, statuses. The
              assistant can read your workspace but cannot change anything yet.
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
