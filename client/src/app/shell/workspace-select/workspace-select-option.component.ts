import { Component, computed, input } from '@angular/core';

const baseClass =
  'hover:bg-hover focus-visible:ring-primary flex h-9.5 w-full cursor-pointer items-center gap-2.5 rounded-[5px] px-2 text-left font-[inherit] text-sm focus-visible:ring-2 focus-visible:outline-none';

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'button[app-workspace-select-option]',
  template: '<ng-content />',
  host: {
    type: 'button',
    '[class]': 'className()',
    '[attr.aria-current]': "current() ? 'true' : null",
    '[attr.data-active]': "active() ? 'true' : null",
  },
})
export class WorkspaceSelectOptionComponent {
  /** The row the arrow keys have moved to. */
  readonly active = input(false);
  /** The workspace the user is already in. */
  readonly current = input(false);

  readonly className = computed(() => {
    if (this.current()) {
      return `${baseClass} bg-primary/14 text-[rgb(var(--foreground-rgb))]`;
    }

    if (this.active()) {
      return `${baseClass} bg-hover text-[rgba(var(--foreground-rgb),0.9)]`;
    }

    return `${baseClass} text-[rgba(var(--foreground-rgb),0.72)]`;
  });
}
