import { Component, input } from '@angular/core';
import { TimelineDependency } from './timeline.models';

@Component({
  selector: 'app-timeline-dependencies',
  host: { class: 'contents' },
  template: `
    <svg
      class="pointer-events-none absolute inset-0 z-5 overflow-visible"
      [attr.width]="width()"
      [attr.height]="height()"
      [attr.viewBox]="'0 0 ' + width() + ' ' + height()"
      aria-hidden="true">
      <defs>
        <marker
          id="timeline-dependency-arrow"
          markerWidth="6"
          markerHeight="6"
          refX="5"
          refY="3"
          orient="auto"
          markerUnits="strokeWidth">
          <path d="M 0 0 L 6 3 L 0 6 z" class="fill-red-500/70" />
        </marker>
      </defs>
      @for (dependency of dependencies(); track dependency.id) {
        <path
          class="stroke-red-500/55"
          fill="none"
          stroke-width="1.5"
          marker-end="url(#timeline-dependency-arrow)"
          [attr.d]="path(dependency)" />
      }
    </svg>
  `,
})
export class TimelineDependenciesComponent {
  readonly dependencies = input<TimelineDependency[]>([]);
  readonly width = input.required<number>();
  readonly height = input.required<number>();

  path(dependency: TimelineDependency): string {
    const direction = dependency.targetX >= dependency.sourceX ? 1 : -1;
    const distance = Math.abs(dependency.targetX - dependency.sourceX);
    const controlOffset = Math.max(18, Math.min(80, distance / 2));
    const sourceControlX = dependency.sourceX + controlOffset * direction;
    const targetControlX = dependency.targetX - controlOffset * direction;

    return [
      `M ${dependency.sourceX} ${dependency.sourceY}`,
      `C ${sourceControlX} ${dependency.sourceY}`,
      `${targetControlX} ${dependency.targetY}`,
      `${dependency.targetX} ${dependency.targetY}`,
    ].join(' ');
  }
}
