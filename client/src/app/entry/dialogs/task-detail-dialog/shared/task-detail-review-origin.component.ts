import { DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { LucideSparkles } from '@lucide/angular';
import { TextLinkComponent } from '@static/components/text-link.component';
import { FromNowPipe } from '@static/pipes/from-now.pipe';
import { TaskDetailService } from '../task-detail.service';
import { TaskDetailReviewService } from './task-detail-review.service';

@Component({
  selector: 'app-task-detail-review-origin',
  imports: [FromNowPipe, LucideSparkles, TextLinkComponent],
  template: `
    @if (origin(); as origin) {
      <p class="text-muted flex items-center gap-1.5 text-xs">
        <svg lucideSparkles class="h-3.5 w-3.5 shrink-0"></svg>
        <span
          i18n="
            Shown on a task created from a suggested split. TASK is the task it
            came from, NAME who accepted the suggestion, TIME is relative
          ">
          Created from a review of
          <button app-text-link type="button" (click)="openParent()">
            {{
              origin.parentSystemId ?? '' // i18n(ph="TASK")
            }}
          </button>
          · accepted by
          {{
            origin.acceptedByDisplayName ?? '' // i18n(ph="NAME")
          }}
          {{
            origin.acceptedAt | fromNow // i18n(ph="TIME")
          }}
        </span>
      </p>
    }
  `,
})
export class TaskDetailReviewOriginComponent {
  private readonly review = inject(TaskDetailReviewService);
  private readonly taskDetail = inject(TaskDetailService);
  private readonly router = inject(Router);
  private readonly dialogRef = inject(DialogRef, { optional: true });

  readonly origin = computed(() => this.review.latest()?.origin ?? null);

  openParent() {
    const workspaceKey = this.taskDetail.task()?.workspaceKey;
    const parentSystemId = this.origin()?.parentSystemId;

    if (!workspaceKey || !parentSystemId) return;

    this.dialogRef?.close();

    void this.router.navigate(['/', workspaceKey, 'tasks', parentSystemId]);
  }
}
