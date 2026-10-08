import { computed, Signal, signal } from '@angular/core';
import { PinnedTask, TaskPin, TaskPinScope } from '@core/models/task-pin';
import { type TabItem } from '@static/components/tab-group/tab-group.component';

export type PinnedFilter = 'all' | 'yours' | 'shared';

const isPersonal = (pin: TaskPin): boolean => pin.scope === TaskPinScope.user;

// The All / Yours / Shared split both pinned lists offer. "Yours" is what you pinned
// for yourself, "Shared" what is pinned for others to see; a task pinned both ways is
// in each.
export function pinnedTaskFilter(pinnedTasks: Signal<PinnedTask[]>) {
  const filter = signal<PinnedFilter>('all');

  const yours = computed(() => {
    return pinnedTasks().filter((pinned) => pinned.pins.some(isPersonal));
  });

  const shared = computed(() => {
    return pinnedTasks().filter((pinned) => {
      return pinned.pins.some((pin) => !isPersonal(pin));
    });
  });

  const visible = computed(() => {
    switch (filter()) {
      case 'yours':
        return yours();
      case 'shared':
        return shared();
      default:
        return pinnedTasks();
    }
  });

  const tabs = computed<TabItem[]>(() => {
    return [
      {
        value: 'all',
        label: $localize`:Pinned task filter showing every pin:All`,
        count: pinnedTasks().length,
      },
      {
        value: 'yours',
        label: $localize`:Pinned task filter showing only your own pins:Yours`,
        count: yours().length,
      },
      {
        value: 'shared',
        label: $localize`:Pinned task filter showing only shared pins:Shared`,
        count: shared().length,
      },
    ];
  });

  return { filter, tabs, visible };
}
