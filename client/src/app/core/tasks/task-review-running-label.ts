import { inject } from '@angular/core';
import { TaskReviewRunsService } from '@core/services/task-review-runs.service';
import { aiReviewRunningLabel } from '@core/util/ai-review-copy';

// Call in an injection context. The returned function reads the running map as it
// goes, so calling it from a signal-tracked expression keeps the mark current.
export function taskReviewRunningLabel(): (taskId: number) => string | null {
  const runs = inject(TaskReviewRunsService);

  return (taskId: number) => {
    return runs.isRunning(taskId) ? aiReviewRunningLabel : null;
  };
}
