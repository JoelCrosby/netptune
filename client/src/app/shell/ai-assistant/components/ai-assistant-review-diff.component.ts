import { Component, computed, input } from '@angular/core';
import { AiChangeField } from '@core/models/ai-conversation';
import { fieldLabel } from './ai-assistant-change-group';
import {
  AiDiffMode,
  diffStat,
  lineOps,
  splitRows,
  unifiedLines,
  wordSegments,
} from './ai-assistant-diff';

/** One field of a proposed change, rendered as a diff in the reviewer's chosen mode. */
@Component({
  selector: 'app-ai-assistant-review-diff',
  host: { class: 'block' },
  template: `
    <div class="border-border bg-card overflow-hidden rounded-lg border">
      <div
        class="border-border bg-card-header flex items-center gap-2 border-b px-2.5 py-1.5">
        <span class="font-avatar text-xs">{{ label() }}</span>
        <span class="text-muted font-avatar text-[11px]">{{
          stat().label
        }}</span>
        <span class="flex-1"></span>
        <span class="text-muted text-[11px]">{{ modeLabel() }}</span>
      </div>

      @switch (mode()) {
        @case ('split') {
          <div
            class="font-avatar grid grid-cols-[minmax(0,1fr)_1px_minmax(0,1fr)] text-[12.5px] leading-relaxed">
            <div>
              <div
                class="border-border text-muted border-b px-2.5 py-1.5 text-[11px] tracking-wide uppercase"
                i18n="Column heading above the current value of a field">
                Before
              </div>
              @for (row of rows(); track $index) {
                <div
                  class="flex gap-2.5 px-2.5 break-words whitespace-pre-wrap"
                  [class.bg-diff-del]="row.beforeKind === 'removed'"
                  [class.bg-hover]="row.beforeKind === null">
                  <span
                    class="text-muted/50 w-6 shrink-0 text-right select-none">
                    {{ row.beforeNumber }}
                  </span>
                  <span class="text-muted min-w-0">{{ row.before }}</span>
                </div>
              }
            </div>

            <div class="bg-border"></div>

            <div>
              <div
                class="border-border text-muted border-b px-2.5 py-1.5 text-[11px] tracking-wide uppercase"
                i18n="Column heading above the proposed value of a field">
                After
              </div>
              @for (row of rows(); track $index) {
                <div
                  class="flex gap-2.5 px-2.5 break-words whitespace-pre-wrap"
                  [class.bg-diff-add]="row.afterKind === 'added'"
                  [class.bg-hover]="row.afterKind === null">
                  <span
                    class="text-muted/50 w-6 shrink-0 text-right select-none">
                    {{ row.afterNumber }}
                  </span>
                  <span class="min-w-0">{{ row.after }}</span>
                </div>
              }
            </div>
          </div>
        }
        @case ('unified') {
          <div class="font-avatar py-1 text-[12.5px] leading-relaxed">
            @for (line of lines(); track $index) {
              <div
                class="flex gap-2.5 px-2.5 break-words whitespace-pre-wrap"
                [class.bg-diff-add]="line.kind === 'added'"
                [class.bg-diff-del]="line.kind === 'removed'">
                <span
                  class="w-3 shrink-0 select-none"
                  [class.text-change-added]="line.kind === 'added'"
                  [class.text-change-removed]="line.kind === 'removed'"
                  [class.text-transparent]="line.kind === 'context'">
                  {{ line.mark }}
                </span>
                <span
                  class="min-w-0"
                  [class.text-muted]="line.kind === 'removed'">
                  {{ line.text }}
                </span>
              </div>
            }
          </div>
        }
        @default {
          <p
            class="m-0 px-3.5 py-3 text-[13.5px] leading-relaxed break-words whitespace-pre-wrap">
            @for (segment of segments(); track $index) {
              <span
                class="rounded-sm"
                [class.bg-diff-add-word]="segment.kind === 'added'"
                [class.bg-diff-del-word]="segment.kind === 'removed'"
                [class.text-muted]="segment.kind === 'removed'"
                [class.line-through]="segment.kind === 'removed'">
                {{ segment.value }}
              </span>
            }
          </p>
        }
      }
    </div>
  `,
})
export class AiAssistantReviewDiffComponent {
  readonly field = input.required<AiChangeField>();
  readonly mode = input<AiDiffMode>('split');

  protected readonly label = computed(() => fieldLabel(this.field().name));

  private readonly ops = computed(() => lineOps(this.field()));

  protected readonly rows = computed(() => splitRows(this.ops()));
  protected readonly lines = computed(() => unifiedLines(this.ops()));
  protected readonly segments = computed(() => wordSegments(this.field()));
  protected readonly stat = computed(() => diffStat(this.ops()));

  protected readonly modeLabel = computed(() => {
    const mode = this.mode();

    if (mode === 'split') {
      return $localize`:Names the side by side diff layout:Side by side`;
    }

    if (mode === 'unified') {
      return $localize`:Names the single column diff layout:Unified`;
    }

    return $localize`:Names the diff layout that highlights changed words:Word level`;
  });
}
