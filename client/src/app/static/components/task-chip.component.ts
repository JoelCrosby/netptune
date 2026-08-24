import { Component, input, output } from '@angular/core';
import {
  LucideDynamicIcon,
  LucideX,
  type LucideIconInput,
} from '@lucide/angular';
import { IconButtonComponent } from './button/icon-button.component';
import { TaskScopeIdComponent } from './task-scope-id.component';

// A task reduced to a chip: scope id, truncating name, and an optional trailing mark. The remove
// control only appears on hover so a row of chips reads as content rather than as controls.
@Component({
  selector: 'app-task-chip',
  imports: [
    IconButtonComponent,
    LucideDynamicIcon,
    LucideX,
    TaskScopeIdComponent,
  ],
  host: {
    class:
      'group border-border bg-board-group-card flex max-w-61 items-center gap-2 rounded-lg border py-1.25 pr-1.5 pl-1.5',
  },
  template: `
    <button
      type="button"
      class="flex min-w-0 cursor-pointer items-center gap-2"
      (click)="opened.emit()">
      <app-task-scope-id [id]="systemId()" />
      <span class="text-foreground truncate text-[13px]">{{ name() }}</span>
    </button>

    @if (icon(); as icon) {
      <svg
        [lucideIcon]="icon"
        class="text-foreground/40 h-3.25 w-3.25 flex-none"
        [title]="iconLabel()"></svg>
    }

    @if (removable()) {
      <button
        app-icon-button
        class="bg-foreground/8 text-foreground/60 hover:bg-foreground/15 invisible h-4.5 w-4.5 flex-none group-hover:visible"
        [ariaLabel]="removeLabel()"
        [title]="removeLabel()"
        (click)="removed.emit()">
        <svg lucideX class="h-2.75 w-2.75"></svg>
      </button>
    }
  `,
})
export class TaskChipComponent {
  readonly systemId = input.required<string>();
  readonly name = input.required<string>();
  readonly icon = input<LucideIconInput | null>(null);
  readonly iconLabel = input('');
  readonly removable = input(false);
  readonly removeLabel = input(
    $localize`:Tooltip on the control that removes an item from a list:Remove`
  );

  readonly opened = output();
  readonly removed = output();
}
