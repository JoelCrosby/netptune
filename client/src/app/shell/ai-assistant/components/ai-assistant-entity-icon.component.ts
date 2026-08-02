import { Component, input } from '@angular/core';
import {
  LucideCalendarRange,
  LucideCircleDashed,
  LucideFolder,
  LucideKanban,
  LucideMessageSquare,
  LucideSquareCheckBig,
  LucideTag,
} from '@lucide/angular';

@Component({
  selector: 'app-ai-assistant-entity-icon',
  imports: [
    LucideCalendarRange,
    LucideCircleDashed,
    LucideFolder,
    LucideKanban,
    LucideMessageSquare,
    LucideSquareCheckBig,
    LucideTag,
  ],
  template: `
    @switch (entityType()) {
      @case ('task') {
        <svg lucideSquareCheckBig [class]="iconClass()"></svg>
      }
      @case ('project') {
        <svg lucideFolder [class]="iconClass()"></svg>
      }
      @case ('sprint') {
        <svg lucideCalendarRange [class]="iconClass()"></svg>
      }
      @case ('board') {
        <svg lucideKanban [class]="iconClass()"></svg>
      }
      @case ('boardGroup') {
        <svg lucideKanban [class]="iconClass()"></svg>
      }
      @case ('tag') {
        <svg lucideTag [class]="iconClass()"></svg>
      }
      @case ('comment') {
        <svg lucideMessageSquare [class]="iconClass()"></svg>
      }
      @default {
        <svg lucideCircleDashed [class]="iconClass()"></svg>
      }
    }
  `,
})
export class AiAssistantEntityIconComponent {
  readonly entityType = input.required<string>();
  readonly iconClass = input('h-3 w-3');
}
