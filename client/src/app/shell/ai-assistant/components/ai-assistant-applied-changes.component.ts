import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AiChangeSet } from '@core/models/ai-conversation';
import { LucideExternalLink, LucideTriangleAlert } from '@lucide/angular';
import { AiAppliedRow, appliedRows } from './ai-assistant-change-summary';
import { letterColour } from './ai-assistant-diff';

@Component({
  selector: 'app-ai-assistant-applied-changes',
  host: { class: 'flex flex-col' },
  imports: [RouterLink, LucideExternalLink, LucideTriangleAlert],
  template: `
    @for (row of rows(); track row.change.id) {
      <div
        class="border-border/55 flex items-start gap-2.25 border-b px-3 py-2 last:border-b-0">
        @if (row.isFailed) {
          <svg
            lucideTriangleAlert
            class="text-change-removed mt-0.5 h-3.5 w-3.5 shrink-0"></svg>
        } @else {
          <span
            class="font-avatar mt-px w-3.5 shrink-0 text-[12px] font-bold"
            [class]="colour(row)"
            aria-hidden="true">
            {{ row.letter }}
          </span>
        }

        <span class="flex min-w-0 flex-1 flex-col gap-0.5">
          <span
            class="text-muted min-w-0 truncate text-[13px]"
            [class.line-through]="isUndone(row)"
            [title]="row.label"
            >{{ row.lead
            }}<span class="text-foreground">{{ row.emphasis }}</span></span
          >

          @if (row.message; as message) {
            <span class="text-change-removed text-[11.5px] break-words">
              {{ message }}
            </span>
          }
        </span>

        @if (row.status; as status) {
          <span
            class="shrink-0 text-[11.5px]"
            [class]="row.isFailed ? 'text-change-removed' : 'text-muted'">
            {{ status }}
          </span>
        }

        @if (row.route; as route) {
          <a
            class="text-primary flex shrink-0 items-center gap-1 text-[11.5px] hover:underline"
            [routerLink]="route"
            [attr.aria-label]="openLabel(row)">
            <span i18n="Link that opens the entity a change changed">Open</span>
            <svg lucideExternalLink class="h-3 w-3"></svg>
          </a>
        }
      </div>
    }
  `,
})
export class AiAssistantAppliedChangesComponent {
  readonly changeSet = input.required<AiChangeSet>();
  readonly workspace = input<string | null>(null);

  protected readonly rows = computed<AiAppliedRow[]>(() => {
    return appliedRows(this.changeSet().changes, this.workspace());
  });

  protected colour(row: AiAppliedRow): string {
    return letterColour(row.letter);
  }

  protected isUndone(row: AiAppliedRow): boolean {
    return !!row.change.undoneAt;
  }

  protected openLabel(row: AiAppliedRow): string {
    return $localize`:Accessible label of the link opening what a change changed:Open ${row.label}:CHANGE:`;
  }
}
