export interface ReportChartTheme {
  primary: string;
  foreground: string;
  mutedForeground: string;
  border: string;
}

export const REPORT_CHART_LABEL_STYLE = {
  cssClass: 'fill-muted',
  fontSize: '11px',
};

const REPORT_VALUE_FORMATTER = new Intl.NumberFormat(undefined, {
  maximumFractionDigits: 1,
});

export function formatReportValue(value: number): string {
  return REPORT_VALUE_FORMATTER.format(value);
}

export function readReportChartTheme(): ReportChartTheme {
  const styles = getComputedStyle(document.documentElement);
  const color = (name: string) => styles.getPropertyValue(name).trim();

  return {
    primary: color('--primary'),
    foreground: color('--foreground'),
    mutedForeground: color('--muted'),
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
