import { DestroyRef, Signal, inject, signal } from '@angular/core';

export function todaySignal(): Signal<Date> {
  const today = signal(localMidnight(new Date()));
  let timer = scheduleRollover();

  inject(DestroyRef).onDestroy(() => clearTimeout(timer));

  return today.asReadonly();

  function scheduleRollover(): ReturnType<typeof setTimeout> {
    const now = new Date();
    const tomorrow = localMidnight(now, 1);

    return setTimeout(() => {
      today.set(localMidnight(new Date()));
      timer = scheduleRollover();
    }, tomorrow.getTime() - now.getTime());
  }
}

function localMidnight(date: Date, dayOffset = 0): Date {
  return new Date(
    date.getFullYear(),
    date.getMonth(),
    date.getDate() + dayOffset
  );
}
