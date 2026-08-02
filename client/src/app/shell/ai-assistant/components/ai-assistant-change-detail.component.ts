import { Component, computed, input } from '@angular/core';
import { AiChangeField, AiProposedChange } from '@core/models/ai-conversation';
import { fieldLabel, isProseField } from './ai-assistant-change-group';
import {
  AiChangeKind,
  AiValueDiff,
  changeKind,
  valueDiff,
} from './ai-assistant-change-kind';

interface AiDetailRow {
  name: string;
  label: string;
  before: string | null;
  after: string | null;
  isProse: boolean;
}

interface AiCollectionRow {
  name: string;
  label: string;
  diff: AiValueDiff;
  isEmpty: boolean;
}

@Component({
  selector: 'app-ai-assistant-change-detail',
  host: { class: 'block text-xs' },
  template: `
    @switch (kind()) {
      @case ('create') {
        <dl class="grid grid-cols-[auto_minmax(0,1fr)] gap-x-3 gap-y-1">
          @for (row of rows(); track row.name) {
            <dt class="text-muted">{{ row.label }}</dt>
            <dd class="min-w-0 break-words whitespace-pre-wrap">
              @if (row.after) {
                {{ row.after }}
              } @else {
                <span class="text-muted" aria-hidden="true">—</span>
              }
            </dd>
          }
        </dl>
      }
      @case ('delete') {
        <ul class="flex flex-col gap-1">
          @for (row of rows(); track row.name) {
            <li class="flex flex-wrap items-baseline gap-1.5">
              <span class="text-muted">{{ row.label }}</span>
              @if (row.before) {
                <span class="text-warn line-through">{{ row.before }}</span>
              }
              @if (row.after) {
                <span class="min-w-0 break-words">{{ row.after }}</span>
              }
            </li>
          }
        </ul>
      }
      @case ('collection') {
        <div class="flex flex-col gap-1.5">
          @for (row of collectionRows(); track row.name) {
            <div class="flex flex-wrap items-center gap-1">
              <span class="text-muted mr-0.5">{{ row.label }}</span>
              @for (value of row.diff.kept; track value) {
                <span class="bg-foreground/10 rounded-full px-2 py-0.5">
                  {{ value }}
                </span>
              }
              @for (value of row.diff.added; track value) {
                <span
                  class="rounded-full bg-green-500/10 px-2 py-0.5 text-green-600 dark:text-green-400">
                  {{ value }}
                </span>
              }
              @for (value of row.diff.removed; track value) {
                <span
                  class="bg-warn/10 text-warn rounded-full px-2 py-0.5 line-through">
                  {{ value }}
                </span>
              }
              @if (row.isEmpty) {
                <span
                  class="text-muted italic"
                  i18n="Shown when a change removes a value">
                  cleared
                </span>
              }
            </div>
          }
        </div>
      }
      @case ('comment') {
        @for (row of rows(); track row.name) {
          <blockquote
            class="border-border text-foreground/80 border-l-2 pl-3 whitespace-pre-wrap">
            {{ row.after }}
          </blockquote>
        }
      }
      @default {
        <ul class="flex flex-col gap-1">
          @for (row of rows(); track row.name) {
            @if (row.isProse) {
              <li class="flex flex-col gap-0.5">
                <span class="text-muted">{{ row.label }}</span>
                @if (row.before) {
                  <span class="text-muted whitespace-pre-wrap line-through">
                    {{ row.before }}
                  </span>
                }
                @if (row.after) {
                  <span class="whitespace-pre-wrap">{{ row.after }}</span>
                } @else {
                  <span
                    class="text-muted italic"
                    i18n="Shown when a change removes a value">
                    cleared
                  </span>
                }
              </li>
            } @else {
              <li class="flex flex-wrap items-baseline gap-1.5">
                <span class="text-muted">{{ row.label }}</span>
                @if (row.before) {
                  <span class="text-muted line-through">{{ row.before }}</span>
                  <span class="text-muted" aria-hidden="true">→</span>
                }
                @if (row.after) {
                  <span class="font-medium">{{ row.after }}</span>
                } @else {
                  <span
                    class="text-muted italic"
                    i18n="Shown when a change removes a value">
                    cleared
                  </span>
                }
              </li>
            }
          }
        </ul>
      }
    }
  `,
})
export class AiAssistantChangeDetailComponent {
  readonly change = input.required<AiProposedChange>();

  protected readonly kind = computed<AiChangeKind>(() => {
    return changeKind(this.change());
  });

  protected readonly rows = computed<AiDetailRow[]>(() => {
    return this.change().fields.map((field) => {
      return {
        name: field.name,
        label: fieldLabel(field.name),
        before: field.before ?? null,
        after: field.after ?? null,
        isProse: isProseField(field),
      };
    });
  });

  protected readonly collectionRows = computed<AiCollectionRow[]>(() => {
    return this.change().fields.map((field) => {
      return this.toCollectionRow(field);
    });
  });

  private toCollectionRow(field: AiChangeField): AiCollectionRow {
    const diff = valueDiff(field);
    const total = diff.kept.length + diff.added.length + diff.removed.length;

    return {
      name: field.name,
      label: fieldLabel(field.name),
      diff,
      isEmpty: total === 0,
    };
  }
}
