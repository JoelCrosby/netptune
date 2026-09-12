import { Component, input } from '@angular/core';

@Component({
  selector: 'app-workspaces-header',
  host: { class: 'flex items-start gap-4' },
  template: `
    <div class="min-w-0 flex-1">
      @if (eyebrow(); as eyebrow) {
        <p
          class="text-foreground/45 mb-1 truncate text-xs font-semibold tracking-[.08em] uppercase">
          {{ eyebrow }}
        </p>
      }

      <h1 class="font-overpass text-3xl font-bold tracking-[-.3px]">
        {{ heading() }}
      </h1>

      <p class="text-foreground/50 mt-1.5 text-[13px]">
        <ng-content select="[headerSubtitle]" />
      </p>
    </div>

    <ng-content select="[headerActions]" />
  `,
})
export class WorkspacesHeaderComponent {
  readonly heading = input.required<string>();
  readonly eyebrow = input<string | null>(null);
}
