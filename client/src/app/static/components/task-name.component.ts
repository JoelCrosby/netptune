import { NgTemplateOutlet } from '@angular/common';
import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideMessageSquareText } from '@lucide/angular';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { TaskFlagBadgeComponent } from './task-flag-badge.component';

@Component({
  selector: 'app-task-name',
  imports: [
    NgTemplateOutlet,
    RouterLink,
    LucideMessageSquareText,
    TaskFlagBadgeComponent,
    TooltipDirective,
  ],
  template: `
    @if (link(); as route) {
      <a
        class="flex w-full items-center gap-2 truncate font-medium hover:underline"
        [routerLink]="route">
        <ng-container [ngTemplateOutlet]="content" />
      </a>
    } @else if (action(); as onClick) {
      <button
        class="flex w-full cursor-pointer items-center gap-2 truncate text-left font-medium hover:underline"
        type="button"
        (click)="onClick()">
        <ng-container [ngTemplateOutlet]="content" />
      </button>
    } @else {
      <span class="flex w-full items-center gap-2 truncate font-medium">
        <ng-container [ngTemplateOutlet]="content" />
      </span>
    }

    <ng-template #content>
      <span class="truncate">{{ name() }}</span>
      @if (hasComments()) {
        <svg
          lucideMessageSquareText
          class="text-muted h-4 w-4 shrink-0"
          i18n-aria-label="
            Accessible label for the icon marking a task that has comments
          "
          aria-label="Has comments"
          i18n-appTooltip="Tooltip on the icon marking a task that has comments"
          appTooltip="Has comments"></svg>
      }
      @if (flagNames().length) {
        <app-task-flag-badge
          [count]="flagNames().length"
          [names]="flagNames()" />
      }
    </ng-template>
  `,
})
export class TaskNameComponent {
  readonly name = input.required<string>();
  readonly link = input<unknown[] | null>(null);
  readonly action = input<(() => void) | null>(null);
  readonly hasComments = input(false);
  readonly flagNames = input<readonly string[]>([]);
}
