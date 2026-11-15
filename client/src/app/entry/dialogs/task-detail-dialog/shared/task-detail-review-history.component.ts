import { Component, computed, inject, signal } from '@angular/core';
import { AiReviewRun, AiReviewRunStatus } from '@core/models/ai-review';
import {
  taskReviewHistoryResource,
  taskReviewRunResource,
} from '@core/resources/ai-review.resource';
import {
  aiReviewCategoryColors,
  aiReviewCategoryLabels,
  aiReviewErrorMessage,
  aiReviewToolCallSummary,
  aiReviewToolSteps,
} from '@core/util/ai-review-copy';
import { formatUsage } from '@core/util/ai-usage';
import { fromNow } from '@core/util/dates';
import { CalloutComponent } from '@static/components/callout/callout.component';
import {
  EntryListComponent,
  EntryListItem,
} from '@static/components/entry-list/entry-list.component';
import { LabelledCardComponent } from '@static/components/labelled-card/labelled-card.component';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import { StepLogComponent } from '@static/components/step-log/step-log.component';
import { TextLinkComponent } from '@static/components/text-link.component';
import { TaskDetailService } from '../task-detail.service';
import { AiReviewEvidenceComponent } from './ai-review-evidence.component';
import { AiReviewUnreadSourcesComponent } from './ai-review-unread-sources.component';

@Component({
  selector: 'app-task-detail-review-history',
  imports: [
    AiReviewEvidenceComponent,
    AiReviewUnreadSourcesComponent,
    CalloutComponent,
    EntryListComponent,
    LabelledCardComponent,
    SpinnerComponent,
    StepLogComponent,
    TextLinkComponent,
  ],
  host: { class: 'flex flex-col gap-4' },
  template: `
    @if (selectedRun(); as selected) {
      <button
        app-text-link
        size="small"
        type="button"
        class="self-start"
        (click)="selectedRunId.set(null)">
        <span i18n="Returns from a past review to the list of reviews">
          Back to review history
        </span>
      </button>

      @if (selectedSteps().length) {
        <app-step-log [summary]="selectedSummary()" [items]="selectedSteps()" />
      }

      <app-ai-review-unread-sources [toolCalls]="selected.toolCalls ?? []" />

      @if (selected.status === failed) {
        <app-callout color="warn">{{ errorMessage(selected) }}</app-callout>
      } @else if (detail(); as detail) {
        @if (detail.run.summary; as summary) {
          <p class="text-[14px]/[1.5]">{{ summary }}</p>
        }
        @for (finding of detail.findings; track finding.id) {
          <app-labelled-card
            [label]="categoryLabels[finding.category]"
            [labelColor]="categoryColors[finding.category]"
            [title]="finding.title"
            [body]="finding.explanation"
            [quote]="finding.quote ?? null">
            <app-ai-review-evidence [evidence]="finding.evidence ?? []" />
          </app-labelled-card>
        } @empty {
          <p
            class="text-muted text-sm"
            i18n="Shown for a past review with no findings">
            This review had no findings.
          </p>
        }
      } @else {
        <app-spinner diameter="20" />
      }
    } @else {
      <app-entry-list
        i18n-ariaLabel="Accessible label of the list of past reviews of a task"
        ariaLabel="Past reviews"
        [items]="entries()"
        (selected)="selectedRunId.set($event)" />
    }
  `,
})
export class TaskDetailReviewHistoryComponent {
  private readonly taskDetail = inject(TaskDetailService);

  private readonly history = taskReviewHistoryResource(this.taskDetail.taskId);

  readonly selectedRunId = signal<number | null>(null);
  readonly failed = AiReviewRunStatus.failed;
  readonly categoryLabels = aiReviewCategoryLabels;
  readonly categoryColors = aiReviewCategoryColors;

  readonly runs = computed(() => {
    return this.history.hasValue() ? this.history.value() : [];
  });

  readonly entries = computed<EntryListItem[]>(() => {
    return this.runs().map((run) => ({
      id: run.id,
      label: runLabel(run),
      meta: this.runMeta(run),
    }));
  });

  readonly selectedRun = computed(() => {
    const runId = this.selectedRunId();

    return this.runs().find((run) => run.id === runId) ?? null;
  });

  readonly selectedSteps = computed(() => {
    return aiReviewToolSteps(this.selectedRun()?.toolCalls ?? []);
  });

  readonly selectedSummary = computed(() => {
    return aiReviewToolCallSummary(this.selectedRun()?.toolCalls ?? []);
  });

  private readonly runRequest = computed(() => {
    const taskId = this.taskDetail.taskId();
    const run = this.selectedRun();
    const hasFindings = run?.status === AiReviewRunStatus.succeeded;

    return taskId && run && hasFindings ? { taskId, runId: run.id } : undefined;
  });

  private readonly runResource = taskReviewRunResource(this.runRequest);

  readonly detail = computed(() => {
    return this.runResource.hasValue() ? this.runResource.value() : undefined;
  });

  errorMessage(run: AiReviewRun): string {
    return aiReviewErrorMessage(run.errorCode);
  }

  private runMeta(run: AiReviewRun): string {
    const outcome = this.runOutcome(run);
    const usage = run.usage ? formatUsage(run.usage) : null;

    return [outcome, usage].filter((part) => !!part).join(' · ');
  }

  private runOutcome(run: AiReviewRun): string {
    const isFailed = run.status === AiReviewRunStatus.failed;

    if (isFailed) {
      return this.errorMessage(run);
    }

    return $localize`:Counts of a past review's findings by state. OPEN, ACCEPTED and DISMISSED are numbers, MODEL is the assistant model:${run.openCount}:OPEN: open · ${run.acceptedCount}:ACCEPTED: accepted · ${run.dismissedCount}:DISMISSED: dismissed · ${run.model}:MODEL:`;
  }
}

function runLabel(run: AiReviewRun): string {
  const parts = [fromNow(run.createdAt), run.runByDisplayName];

  return parts.filter((part) => !!part).join(' · ');
}
