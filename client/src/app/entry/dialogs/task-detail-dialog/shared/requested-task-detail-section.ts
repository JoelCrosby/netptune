import { inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { TASK_DETAIL_SECTION_PARAM } from '@core/tasks/task-detail-section';

// Call in an injection context. A notice about a finished review links straight to
// the review, so the section the link asks for decides where the task opens.
export function requestedTaskDetailSection(): string | null {
  const params = inject(ActivatedRoute).snapshot.queryParamMap;

  return params.get(TASK_DETAIL_SECTION_PARAM);
}
