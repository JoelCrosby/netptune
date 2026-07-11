import { SprintStatus } from '@core/enums/sprint-status';

export interface SprintDaysChip {
  label: string;
  classes: string;
}

export function sprintDaysChip(
  status: SprintStatus,
  endDate: string
): SprintDaysChip | null {
  if (status !== SprintStatus.active) return null;

  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const end = new Date(endDate);
  end.setHours(0, 0, 0, 0);
  const diff = Math.ceil((end.getTime() - today.getTime()) / 86_400_000);

  if (diff < 0) {
    return {
      label: `${Math.abs(diff)}d overdue`,
      classes: 'bg-red-100 text-red-700',
    };
  }
  if (diff === 0) {
    return { label: 'Due today', classes: 'bg-orange-100 text-orange-700' };
  }
  if (diff <= 3) {
    return { label: `${diff}d left`, classes: 'bg-orange-100 text-orange-700' };
  }
  return { label: `${diff}d left`, classes: 'bg-neutral-100 text-neutral-600' };
}
