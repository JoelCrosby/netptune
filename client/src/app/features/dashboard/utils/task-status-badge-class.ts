import { StatusCategory } from '@core/models/status';

/**
 * Tailwind classes for a task status badge, keyed by status category. Shared by
 * the dashboard cards so the badges render consistently across the view.
 */
export function taskStatusBadgeClass(status: StatusCategory): string {
  switch (status) {
    case StatusCategory.todo:
      return 'bg-blue-100 text-blue-700';
    case StatusCategory.active:
      return 'bg-yellow-100 text-yellow-700';
    case StatusCategory.done:
      return 'bg-green-100 text-green-700';
    case StatusCategory.backlog:
      return 'bg-purple-100 text-purple-700';
    default:
      return 'bg-neutral-100 text-neutral-600';
  }
}
