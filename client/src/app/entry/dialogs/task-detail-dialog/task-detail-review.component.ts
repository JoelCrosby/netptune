import { DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import {
  AiReviewFinding,
  AiReviewFindingStatus,
  AiReviewReadiness,
  AiReviewReference,
  isAppliedEdit,
} from '@core/models/ai-review';
import {
  aiReviewCategoryColors,
  aiReviewCategoryLabels,
  aiReviewErrorMessage,
  aiReviewReadinessColors,
  aiReviewReadinessLabels,
} from '@core/util/ai-review-copy';
import { DialogService } from '@core/services/dialog.service';
import { formatElapsed } from '@core/util/duration';
import { numberFormat } from '@core/util/locale';
import {
  AiReviewAcceptDialogComponent,
  AiReviewAcceptDialogData,
  AiReviewAcceptDialogResult,
} from '@entry/dialogs/ai-review-accept-dialog/ai-review-accept-dialog.component';
import { LucideHistory, LucideSparkles } from '@lucide/angular';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CalloutComponent } from '@static/components/callout/callout.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { IconTileComponent } from '@static/components/icon-tile.component';
import { LabelledCardComponent } from '@static/components/labelled-card/labelled-card.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { TextLinkComponent } from '@static/components/text-link.component';
import { FromNowPipe } from '@static/pipes/from-now.pipe';
import { TaskDetailReviewHistoryComponent } from './shared/task-detail-review-history.component';
import { TaskDetailReviewService } from './shared/task-detail-review.service';
import { TaskDetailService } from './task-detail.service';

const characterCount = numberFormat();

@Component({
  selector: 'app-task-detail-review',
  imports: [
    BadgeComponent,
    CalloutComponent,
    EmptyStateComponent,
    FlatButtonComponent,
    FormInputComponent,
    FromNowPipe,
    IconTileComponent,
    LabelledCardComponent,
    LucideHistory,
    LucideSparkles,
    StrokedButtonComponent,
    TaskDetailReviewHistoryComponent,
    TaskScopeIdComponent,
    TextLinkComponent,
  ],
  host: { class: 'flex min-h-full flex-col gap-3' },
  template: `
    <div class="flex flex-wrap items-center gap-2">
      @if (current(); as current) {
        <app-badge [color]="readinessColor()">{{ readinessLabel() }}</app-badge>
        <span class="text-muted text-xs">
          <span
            i18n="
              When a task was last reviewed, who ran it and which model. TIME is
              relative, like 2 hours ago
            ">
            Reviewed
            {{
              current.run.createdAt | fromNow // i18n(ph="TIME")
            }}
            by
            {{
              current.run.runByDisplayName ?? unknownPerson // i18n(ph="NAME")
            }}
            ·
            {{
              current.run.model // i18n(ph="MODEL")
            }}
          </span>
        </span>
      }

      @if (canRun()) {
        <button
          class="ml-auto"
          app-stroked-button
          type="button"
          [disabled]="review.running()"
          (click)="review.run()">
          <svg lucideSparkles class="h-3.5 w-3.5"></svg>
          @if (current()) {
            <span i18n="Button that runs the assistant review on a task again"
              >Run again</span
            >
          } @else {
            <span i18n="Button that runs the assistant review on a task"
              >Run review</span
            >
          }
        </button>
      }
    </div>

    @if (review.running()) {
      <div class="flex min-h-40 flex-1 items-center justify-center">
        <app-empty-state
          compact
          busy
          [title]="liveLabel()"
          [description]="liveMeta()">
          <app-icon-tile
            emptyStateIcon
            size="large"
            raised
            [icon]="assistantIcon" />
        </app-empty-state>
      </div>
    } @else {
      @if (review.runError(); as runError) {
        <app-callout color="warn" role="alert">{{ runError }}</app-callout>
      }

      @if (latestFailure(); as failure) {
        <app-callout
          color="warn"
          i18n-title="
            Title of the notice shown when the latest review of a task failed
          "
          title="The latest review failed">
          {{ failureMessage() }}
        </app-callout>
      }

      @if (isStale()) {
        <app-callout
          color="neutral"
          i18n-title="
            Title of the notice shown when a task changed after it was reviewed
          "
          title="This task changed since the review">
          <span i18n="Suggests reviewing a task again after it changed">
            Run the review again to check the latest version.
          </span>
        </app-callout>
      }

      @if (current(); as current) {
        @if (current.run.summary; as summary) {
          <p class="text-sm">{{ summary }}</p>
        }

        @for (finding of review.openFindings(); track finding.id) {
          <app-labelled-card
            [label]="categoryLabel(finding)"
            [labelColor]="categoryColor(finding)"
            [title]="finding.title"
            [body]="finding.explanation"
            [quote]="finding.quote ?? null">
            @if (finding.references.length) {
              <ul class="mt-2 flex flex-col gap-1 text-xs">
                @for (
                  reference of finding.references;
                  track referenceKey(reference)
                ) {
                  @if (reference.systemId) {
                    <li class="flex min-w-0 items-center gap-1.5">
                      <app-task-scope-id [id]="reference.systemId" />
                      <button
                        app-text-link
                        color="inherit"
                        type="button"
                        class="truncate text-left"
                        (click)="openTask(reference)">
                        {{ reference.name }}
                      </button>
                    </li>
                  }
                }
              </ul>
            }

            @if (dismissingFindingId() === finding.id) {
              <div class="mt-3 flex flex-col gap-2">
                <app-form-input
                  density="compact"
                  [noMargin]="true"
                  maxLength="500"
                  i18n-placeholder="
                    Placeholder of the optional reason for dismissing a review
                    finding
                  "
                  placeholder="Why doesn't this apply? (optional)"
                  [(value)]="dismissReason"
                  (keydown.enter)="confirmDismiss(finding)" />
                <div class="flex gap-1">
                  <button
                    app-flat-button
                    type="button"
                    (click)="confirmDismiss(finding)">
                    <span
                      i18n="Button that confirms dismissing a review finding"
                      >Dismiss</span
                    >
                  </button>
                  <button
                    app-stroked-button
                    type="button"
                    (click)="cancelDismiss()">
                    <span i18n="Button that cancels dismissing a review finding"
                      >Cancel</span
                    >
                  </button>
                </div>
              </div>
            }

            @if (canDecide() && dismissingFindingId() !== finding.id) {
              <ng-container cardActions>
                @if (finding.suggestion) {
                  <button
                    app-flat-button
                    type="button"
                    [disabled]="isDeciding(finding)"
                    (click)="openAccept(finding)">
                    <span
                      i18n="
                        Button that previews and applies a review suggestion
                      "
                      >Review suggestion</span
                    >
                  </button>
                }
                <button
                  app-stroked-button
                  type="button"
                  [disabled]="isDeciding(finding)"
                  (click)="startDismiss(finding)">
                  <span i18n="Button that dismisses a review finding"
                    >Dismiss</span
                  >
                </button>
              </ng-container>
            }
          </app-labelled-card>
        } @empty {
          <p
            class="text-muted text-sm"
            i18n="Shown when the latest review of a task left nothing open">
            Nothing is left open in this review.
          </p>
        }

        @if (review.decidedFindings().length; as decidedCount) {
          <button
            app-text-link
            color="muted"
            size="small"
            type="button"
            class="self-start"
            (click)="showDecided.set(!showDecided())">
            @if (showDecided()) {
              <span i18n="Hides review findings that were accepted or dismissed"
                >Hide decided findings</span
              >
            } @else {
              <span
                i18n="Shows review findings that were accepted or dismissed">
                {decidedCount, plural,
                  =1 {Show 1 decided finding}
                  other {Show {{ decidedCount }} decided findings}
                }
              </span>
            }
          </button>

          @if (showDecided()) {
            @for (finding of review.decidedFindings(); track finding.id) {
              <app-labelled-card
                muted
                [label]="categoryLabel(finding)"
                [labelColor]="categoryColor(finding)"
                [title]="finding.title"
                [body]="finding.dismissReason ?? null"
                [status]="decisionLabel(finding)">
                @if (canDecide() && canReopen(finding)) {
                  <ng-container cardActions>
                    @if (canRevert(finding)) {
                      <button
                        app-stroked-button
                        type="button"
                        [disabled]="isDeciding(finding)"
                        (click)="review.reopen(finding, true)">
                        <span
                          i18n="
                            Button that undoes an accepted review suggestion
                          "
                          >Revert</span
                        >
                      </button>
                    }
                    <button
                      app-stroked-button
                      type="button"
                      [disabled]="isDeciding(finding)"
                      (click)="review.reopen(finding)">
                      <span i18n="Button that reopens a decided review finding"
                        >Reopen</span
                      >
                    </button>
                  </ng-container>
                }
              </app-labelled-card>
            }
          }
        }
      } @else if (!review.loading()) {
        <app-empty-state
          compact
          i18n-title="Heading shown when a task has never been reviewed"
          title="No review yet"
          i18n-description="Explains what an assistant review of a task does"
          description="A review checks this task for gaps, unstated assumptions, ambiguity, scope, overlap and testability.">
          <svg emptyStateIcon lucideSparkles></svg>
        </app-empty-state>
      }

      @if (mentions().length) {
        <div class="flex flex-col gap-1">
          <span
            class="text-muted text-xs font-medium"
            i18n="Heading above other reviews that mention this task">
            Mentioned in other reviews
          </span>
          @for (mention of mentions(); track mention.findingId) {
            <div class="flex items-center gap-1.5 text-xs">
              @if (mention.subjectSystemId) {
                <app-task-scope-id [id]="mention.subjectSystemId" />
              }
              <span class="truncate">{{ mention.findingTitle }}</span>
            </div>
          }
        </div>
      }

      @if (review.hasReview()) {
        <button
          app-text-link
          color="muted"
          size="small"
          type="button"
          class="flex items-center gap-1.5 self-start"
          (click)="showHistory.set(!showHistory())">
          <svg lucideHistory class="h-3.5 w-3.5"></svg>
          <span i18n="Toggles the list of past reviews of a task"
            >Review history</span
          >
        </button>

        @if (showHistory()) {
          <app-task-detail-review-history />
        }
      }
    }
  `,
})
export class TaskDetailReviewComponent {
  readonly review = inject(TaskDetailReviewService);

  private readonly taskDetail = inject(TaskDetailService);
  private readonly dialog = inject(DialogService);
  private readonly router = inject(Router);
  private readonly dialogRef = inject(DialogRef, { optional: true });

  private readonly canCreateTags = hasPermission(PERMISSIONS.tags.create);

  readonly canRun = this.taskDetail.canAskAssistant;
  readonly canDecide = hasPermission(PERMISSIONS.tasks.update);

  readonly showDecided = signal(false);
  readonly showHistory = signal(false);
  readonly dismissingFindingId = signal<number | null>(null);
  readonly dismissReason = signal('');

  readonly assistantIcon = LucideSparkles;

  readonly current = this.review.current;
  readonly mentions = computed(() => this.review.latest()?.mentionedIn ?? []);

  readonly isStale = computed(() => this.review.latest()?.isStale ?? false);

  readonly latestFailure = computed(() => {
    const latest = this.review.latest();
    const failure = latest?.latestFailure ?? null;
    const hasRunError = this.review.runError() !== null;

    return hasRunError ? null : failure;
  });

  readonly failureMessage = computed(() => {
    return aiReviewErrorMessage(this.latestFailure()?.errorCode);
  });

  private readonly readiness = computed(() => {
    return this.review.latest()?.readiness ?? AiReviewReadiness.unreviewed;
  });

  readonly readinessLabel = computed(
    () => aiReviewReadinessLabels[this.readiness()]
  );
  readonly readinessColor = computed(
    () => aiReviewReadinessColors[this.readiness()]
  );

  readonly liveLabel = computed(() => {
    const hasOutput = this.review.progress() > 0;

    return hasOutput
      ? $localize`:Shown while the assistant writes out the findings of a task review:Writing up the findings…`
      : $localize`:Shown while the assistant reviews a task, before it starts writing:Reviewing the task…`;
  });

  readonly liveMeta = computed(() => {
    const elapsed = formatElapsed(this.review.elapsedMs());
    const characters = this.review.progress();

    if (characters === 0) return elapsed;

    const count = characterCount.format(characters);

    return $localize`:Task review progress, for example "8s · 1,400 chars":${elapsed}:elapsed: · ${count}:count: chars`;
  });

  readonly unknownPerson = $localize`:Shown in place of a person's name when it is not known:someone`;

  categoryLabel(finding: AiReviewFinding): string {
    return aiReviewCategoryLabels[finding.category];
  }

  categoryColor(finding: AiReviewFinding) {
    return aiReviewCategoryColors[finding.category];
  }

  decisionLabel(finding: AiReviewFinding): string {
    const person = finding.decidedByDisplayName ?? this.unknownPerson;
    const isAccepted = finding.status === AiReviewFindingStatus.accepted;

    return isAccepted
      ? $localize`:Who accepted a review finding. NAME is a person:Accepted by ${person}:NAME:`
      : $localize`:Who dismissed a review finding. NAME is a person:Dismissed by ${person}:NAME:`;
  }

  canRevert(finding: AiReviewFinding): boolean {
    return isAppliedEdit(finding.appliedChange);
  }

  canReopen(finding: AiReviewFinding): boolean {
    const isDismissed = finding.status === AiReviewFindingStatus.dismissed;

    return isDismissed || this.canRevert(finding);
  }

  referenceKey(reference: AiReviewReference): string {
    return `${reference.role}-${reference.entityType}-${reference.entityId}`;
  }

  isDeciding(finding: AiReviewFinding): boolean {
    return this.review.decidingFindingId() === finding.id;
  }

  startDismiss(finding: AiReviewFinding) {
    this.dismissReason.set('');
    this.dismissingFindingId.set(finding.id);
  }

  cancelDismiss() {
    this.dismissingFindingId.set(null);
  }

  async confirmDismiss(finding: AiReviewFinding) {
    const dismissed = await this.review.dismiss(finding, this.dismissReason());

    if (dismissed) {
      this.dismissingFindingId.set(null);
    }
  }

  async openAccept(finding: AiReviewFinding) {
    const suggestion = finding.suggestion;
    const task = this.taskDetail.task();

    if (!suggestion || !task) return;

    const relatedTaskId = suggestion.kind === 'link' ? suggestion.taskId : null;
    const relatedTask = finding.references.find((reference) => {
      return reference.entityId === relatedTaskId;
    });

    const data: AiReviewAcceptDialogData = {
      finding,
      suggestion,
      currentText: task.description ?? '',
      task: {
        tags: task.tags,
        estimateValue: task.estimateValue,
        priority: task.priority,
      },
      relatedTaskName: relatedTask?.name ?? null,
      canCreateTags: this.canCreateTags(),
    };

    const result = await this.dialog.openForResult<AiReviewAcceptDialogResult>(
      AiReviewAcceptDialogComponent,
      { width: '880px', maxWidth: '95vw', data }
    );

    if (!result) return;

    await this.review.accept(finding, result);
  }

  // This tab renders inside both the dialog and the standalone task page; an open dialog has to close
  // before navigating or it would sit over the task we just opened.
  openTask(reference: AiReviewReference) {
    const workspaceKey = this.taskDetail.task()?.workspaceKey;

    if (!workspaceKey || !reference.systemId) return;

    this.dialogRef?.close();

    void this.router.navigate(['/', workspaceKey, 'tasks', reference.systemId]);
  }
}
