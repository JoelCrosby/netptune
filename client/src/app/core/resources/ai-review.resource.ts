import { Signal } from '@angular/core';
import { PERMISSIONS } from '@core/auth/permissions';
import {
  AiReviewLatest,
  AiReviewRun,
  AiReviewRunDetail,
} from '@core/models/ai-review';
import {
  permissionResource,
  requestFrom,
} from '@core/resources/permission.resource';

export const taskReviewResource = (taskId: Signal<number | undefined>) => {
  return permissionResource<AiReviewLatest | undefined>({
    permission: PERMISSIONS.tasks.read,
    request: requestFrom(taskId, (id) => ({
      url: `api/tasks/${id}/reviews/latest`,
    })),
    parse: (response) => response.payload ?? undefined,
    defaultValue: undefined,
    refreshOn: ['aiReviews', 'tasks'],
  });
};

export const taskReviewHistoryResource = (
  taskId: Signal<number | undefined>
) => {
  return permissionResource<AiReviewRun[]>({
    permission: PERMISSIONS.tasks.read,
    request: requestFrom(taskId, (id) => ({
      url: `api/tasks/${id}/reviews`,
    })),
    parse: (response) => response.payload ?? [],
    defaultValue: [],
    refreshOn: ['aiReviews'],
  });
};

export const taskReviewRunResource = (
  request: Signal<{ taskId: number; runId: number } | undefined>
) => {
  return permissionResource<AiReviewRunDetail | undefined>({
    permission: PERMISSIONS.tasks.read,
    request: requestFrom(request, ({ taskId, runId }) => ({
      url: `api/tasks/${taskId}/reviews/${runId}`,
    })),
    parse: (response) => response.payload ?? undefined,
    defaultValue: undefined,
    refreshOn: ['aiReviews'],
  });
};
