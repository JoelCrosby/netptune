export interface ReportChartTheme {
  primary: string;
  foreground: string;
  mutedForeground: string;
  border: string;
}

export function readReportChartTheme(): ReportChartTheme {
  const styles = getComputedStyle(document.documentElement);
  const color = (name: string) =>
    `hsl(${styles.getPropertyValue(name).trim()})`;

  return {
    primary: color('--primary'),
    foreground: color('--foreground'),
    mutedForeground: color('--muted-foreground'),
    border: color('--border'),
  };
}

export function reportChartThemeSignal(): Signal<ReportChartTheme> {
  const destroyRef = inject(DestroyRef);
  const theme = signal(readReportChartTheme());
  const observer = new MutationObserver(() =>
    theme.set(readReportChartTheme())
  );
  observer.observe(document.documentElement, {
    attributes: true,
    attributeFilter: ['class', 'style'],
  });
  destroyRef.onDestroy(() => observer.disconnect());
  return theme.asReadonly();
}
import { DestroyRef, Signal, inject, signal } from '@angular/core';
