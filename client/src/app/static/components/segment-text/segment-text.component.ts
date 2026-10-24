import { Component, booleanAttribute, computed, input } from '@angular/core';
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
  host: { '[class]': 'hostClass()' },
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
  readonly struck = input(false, { transform: booleanAttribute });

  protected readonly hostClass = computed(() => {
    return this.struck() ? 'text-foreground/45 line-through' : '';
  });
}
