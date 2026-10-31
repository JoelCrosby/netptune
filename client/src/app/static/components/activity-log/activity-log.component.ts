import { Component, computed, input, signal } from '@angular/core';
import { LucideCheck, LucideChevronDown, LucideX } from '@lucide/angular';
import { SpinnerIconComponent } from '../spinner/spinner-icon.component';

export interface ActivityLogItem {
  kind: 'note' | 'step';
  text: string;
  state: 'running' | 'done' | 'failed';
}

@Component({
  selector: 'app-activity-log',
  imports: [LucideCheck, LucideChevronDown, LucideX, SpinnerIconComponent],
  host: { class: 'flex min-h-0 flex-col gap-2' },
  template: `
    @if (status()) {
      <div class="mb-1 flex min-w-0 items-center gap-2">
        <app-spinner-icon />
        <span class="truncate text-sm font-medium" aria-live="polite">{{
          status()
        }}</span>
        @if (meta()) {
          <span class="text-muted ml-auto shrink-0 text-xs tabular-nums">{{
            meta()
          }}</span>
        }
      </div>
    }

    @if (items().length) {
      <div class="flex max-h-72 flex-col-reverse overflow-y-auto">
        <ol class="flex flex-col gap-1.5">
          @for (item of items(); track $index) {
            @if (item.kind === 'note') {
              <li class="text-foreground/80 text-sm whitespace-pre-line">
                {{ item.text }}
              </li>
            } @else {
              <li
                class="font-avatar flex min-w-0 items-center gap-1.5 text-[11.5px]"
                [class]="item.state === 'failed' ? 'text-warn' : 'text-muted'">
                @switch (item.state) {
                  @case ('running') {
                    <app-spinner-icon class="h-3.5 w-3.5 shrink-0" />
                  }
                  @case ('done') {
                    <svg
                      lucideCheck
                      class="h-3.5 w-3.5 shrink-0"
                      aria-hidden="true"></svg>
                  }
                  @case ('failed') {
                    <svg
                      lucideX
                      class="h-3.5 w-3.5 shrink-0"
                      aria-hidden="true"></svg>
                  }
                }
                <span class="truncate">{{ item.text }}</span>
                @if (item.state === 'failed') {
                  <span
                    class="shrink-0"
                    i18n="Marks a step in a list of steps that failed"
                    >(failed)</span
                  >
                }
              </li>
            }
          }
        </ol>
      </div>
    }

    @if (current()) {
      <button
        type="button"
        class="text-muted hover:text-foreground focus-visible:ring-primary flex min-w-0 items-start gap-1.5 rounded text-left text-xs transition-colors focus-visible:ring-2 focus-visible:outline-none"
        [attr.aria-expanded]="expanded()"
        (click)="expanded.set(!expanded())">
        <svg
          lucideChevronDown
          class="mt-px h-3.5 w-3.5 shrink-0 transition-transform duration-150"
          [class.-rotate-90]="!expanded()"
          aria-hidden="true"></svg>
        @if (currentLabel()) {
          <span class="text-foreground/70 shrink-0 font-medium">
            {{ currentLabel() }}
          </span>
        }
        @if (expanded()) {
          <span
            class="max-h-40 min-w-0 flex-1 overflow-y-auto whitespace-pre-line italic">
            {{ current() }}
          </span>
        } @else {
          <span class="min-w-0 flex-1 truncate italic">{{
            currentLine()
          }}</span>
        }
      </button>
    }
  `,
})
export class ActivityLogComponent {
  readonly items = input.required<ActivityLogItem[]>();
  readonly status = input('');
  readonly meta = input('');
  readonly current = input('');
  readonly currentLabel = input('');

  readonly expanded = signal(false);

  protected readonly currentLine = computed(() => {
    const paragraphs = this.current().trim().split(/\n+/);

    return paragraphs.at(-1) ?? '';
  });
}
