import { Component, computed, input } from '@angular/core';
import { cn } from '../button/button.variants';
import {
  DiffMode,
  lineDiff,
  splitRows,
  unifiedLines,
  inlineDiff,
} from './text-diff';

@Component({
  selector: 'app-text-diff',
  host: { '[class]': 'hostClass()' },
  template: `
    @switch (mode()) {
      @case ('split') {
        <div
          class="font-avatar grid grid-cols-[minmax(0,1fr)_1px_minmax(0,1fr)] text-[15px] leading-relaxed">
          <div>
            <div
              class="border-border text-muted border-b px-3 py-2 text-xs tracking-wide uppercase">
              {{ beforeLabel() }}
            </div>
            @for (row of rows(); track $index) {
              <div
                class="flex gap-3 px-3 break-words whitespace-pre-wrap"
                [class.bg-diff-del]="row.beforeKind === 'removed'"
                [class.bg-hover]="row.beforeKind === null">
                <span
                  class="text-muted w-7 shrink-0 text-right select-none"
                  [textContent]="row.beforeNumber"></span>
                <span class="text-muted min-w-0">{{ row.before }}</span>
              </div>
            }
          </div>

          <div class="bg-border"></div>

          <div>
            <div
              class="border-border text-muted border-b px-3 py-2 text-xs tracking-wide uppercase">
              {{ afterLabel() }}
            </div>
            @for (row of rows(); track $index) {
              <div
                class="flex gap-3 px-3 break-words whitespace-pre-wrap"
                [class.bg-diff-add]="row.afterKind === 'added'"
                [class.bg-hover]="row.afterKind === null">
                <span
                  class="text-muted w-6 shrink-0 text-right select-none"
                  [textContent]="row.afterNumber"></span>
                <span class="min-w-0">{{ row.after }}</span>
              </div>
            }
          </div>
        </div>
      }
      @case ('unified') {
        <div class="font-avatar py-1.5 text-[15px] leading-relaxed">
          @for (line of lines(); track $index) {
            <div
              class="flex gap-3 px-3 break-words whitespace-pre-wrap"
              [class.bg-diff-add]="line.kind === 'added'"
              [class.bg-diff-del]="line.kind === 'removed'">
              <span
                class="w-3 shrink-0 select-none"
                [class.text-change-added]="line.kind === 'added'"
                [class.text-change-removed]="line.kind === 'removed'"
                [class.text-transparent]="line.kind === 'context'"
                [textContent]="line.mark"></span>
              <span
                class="min-w-0"
                [class.text-muted]="line.kind === 'removed'"
                [textContent]="line.text"></span>
            </div>
          }
        </div>
      }
      @default {
        <p
          class="m-0 px-4 py-3.5 text-[15px] leading-relaxed break-words whitespace-pre-wrap">
          @for (segment of segments(); track $index) {
            <span
              class="rounded-sm"
              [class.bg-diff-add-word]="segment.kind === 'added'"
              [class.bg-diff-del-word]="segment.kind === 'removed'"
              [class.text-muted]="segment.kind === 'removed'"
              [class.line-through]="segment.kind === 'removed'"
              [textContent]="segment.value"></span>
          }
        </p>
      }
    }
  `,
})
export class TextDiffComponent {
  readonly before = input<string | null | undefined>(null);
  readonly after = input<string | null | undefined>(null);
  readonly mode = input<DiffMode>('split');
  readonly beforeLabel = input(
    $localize`:Column heading above the earlier text in a side by side comparison:Before`
  );
  readonly afterLabel = input(
    $localize`:Column heading above the later text in a side by side comparison:After`
  );
  readonly class = input('');

  private readonly ops = computed(() => lineDiff(this.before(), this.after()));

  protected readonly rows = computed(() => splitRows(this.ops()));
  protected readonly lines = computed(() => unifiedLines(this.ops()));
  protected readonly segments = computed(() => {
    return inlineDiff(this.before(), this.after());
  });

  protected readonly hostClass = computed(() => cn('block', this.class()));
}
