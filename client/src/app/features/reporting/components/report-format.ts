export function hoursLabel(value?: number | null): string {
  return value == null ? '—' : `${Math.round(value * 10) / 10}h`;
}
