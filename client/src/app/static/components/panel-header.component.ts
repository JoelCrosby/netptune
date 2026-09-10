import { Component, computed, input } from '@angular/core';
import { type LucideIconInput } from '@lucide/angular';
import { IconCircleComponent } from './icon-circle.component';
import { IconTileComponent } from './icon-tile.component';

// `compact` is the dense toolbar-style header; `comfortable` is the roomier
// settings-card header, which lets its description wrap onto a second line.
export type PanelHeaderDensity = 'compact' | 'comfortable';

@Component({
  selector: 'app-panel-header',
  imports: [IconCircleComponent, IconTileComponent],
  template: `
    <header [class]="headerClass()">
      <div class="flex min-w-0 items-center gap-3">
        @if (icon(); as headerIcon) {
          @if (density() === 'comfortable') {
            <app-icon-tile [icon]="headerIcon" [class]="iconClass()" />
          } @else {
            <app-icon-circle [icon]="headerIcon" [class]="iconClass()" />
          }
        }

        <div class="min-w-0">
          @if (heading()) {
            <h2 [class]="headingClass()">{{ heading() }}</h2>
          }

          @if (description()) {
            <p [class]="descriptionClass()">{{ description() }}</p>
          }

          <ng-content select="[panelHeading]" />
        </div>
      </div>

      <div class="shrink-0 empty:hidden">
        <ng-content select="[panelHeaderActions]" />
      </div>
    </header>
  `,
  styles: ``,
})
export class PanelHeaderComponent {
  // Either pass the text as inputs, or project a `panelHeading` block when the
  // copy needs control flow of its own.
  readonly heading = input('');
  readonly description = input('');
  readonly icon = input<LucideIconInput | undefined>();
  readonly density = input<PanelHeaderDensity>('compact');
  readonly iconClass = input('');

  protected readonly headerClass = computed(() => {
    return this.density() === 'comfortable'
      ? 'border-border flex flex-wrap items-center justify-between gap-x-4 gap-y-3 border-b px-6 py-5'
      : 'border-border bg-foreground/3 flex flex-wrap items-center justify-between gap-3 border-b px-4 py-3';
  });

  protected readonly headingClass = computed(() => {
    return this.density() === 'comfortable'
      ? 'font-overpass text-base font-semibold'
      : 'text-sm font-medium';
  });

  protected readonly descriptionClass = computed(() => {
    return this.density() === 'comfortable'
      ? 'text-muted mt-1 text-sm'
      : 'text-foreground/60 truncate text-xs';
  });
}
