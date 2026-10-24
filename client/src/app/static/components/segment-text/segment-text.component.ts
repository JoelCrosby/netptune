import { Component, input } from '@angular/core';
import { StatusCategory } from '@core/models/status';
import { TaskStatusPillComponent } from '@static/components/task-status-pill.component';

export type TextSegment =
  | { type: 'text'; text: string }
  | {
      type: 'status';
      name: string;
      color?: string | null;
      category?: StatusCategory | null;
    };

@Component({
  selector: 'app-segment-text',
  imports: [TaskStatusPillComponent],
  template: `
    @for (segment of segments(); track $index) {
      @if (segment.type === 'status') {
        <app-task-status-pill
          [name]="segment.name"
          [color]="segment.color"
          [category]="segment.category ?? null" />
      } @else {
        <!-- Segments carry their own spacing, so template whitespace would double it. -->
        <ng-container>{{ segment.text }}</ng-container>
      }
    }
  `,
})
export class SegmentTextComponent {
  readonly segments = input.required<TextSegment[]>();
}
