import { Component, computed, input, model } from '@angular/core';
import { Status } from '@core/models/status';
import { statusResource } from '@core/resources/status.resource';
import { LucideEllipsis } from '@lucide/angular';
import { cn } from '@static/components/button/button.variants';
import { ColorSwatchComponent } from '@static/components/color-swatch/color-swatch.component';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { EYEBROW } from '../task-detail-styles';

const VISIBLE_SEGMENTS = 3;

@Component({
  selector: 'app-task-status-segments',
  imports: [
    ColorSwatchComponent,
    DropdownMenuComponent,
    MenuItemComponent,
    LucideEllipsis,
  ],
  host: { class: 'flex flex-col gap-2.5' },
  template: `
    <div [class]="eyebrowClass" [id]="eyebrowId()">
      <span i18n="Field heading for the task status">Status</span>
    </div>

    <div
      class="border-foreground/8 flex h-8 items-stretch overflow-hidden rounded-lg border"
      role="group"
      [attr.aria-labelledby]="eyebrowId()">
      @for (status of segments(); track status.id; let first = $first) {
        <button
          type="button"
          [class]="segmentClass(status.id === value(), first)"
          [attr.aria-pressed]="status.id === value()"
          [disabled]="disabled()"
          (click)="value.set(status.id)">
          {{ status.name }}
        </button>
      }

      @if (overflow().length) {
        <button
          #overflowButton
          type="button"
          [class]="overflowClass()"
          aria-haspopup="menu"
          i18n-aria-label="
            Accessible label for the control that lists the remaining statuses
          "
          aria-label="More statuses"
          [disabled]="disabled()"
          (click)="menu.toggle(overflowButton)">
          <svg lucideEllipsis class="h-4 w-4"></svg>
        </button>

        <app-dropdown-menu #menu xPosition="before">
          @for (status of overflow(); track status.id) {
            <button
              app-menu-item
              [disabled]="status.id === value()"
              (click)="value.set(status.id); menu.close()">
              @if (status.color) {
                <app-color-swatch [color]="status.color" />
              }
              {{ status.name }}
            </button>
          }
        </app-dropdown-menu>
      }
    </div>
  `,
})
export class TaskStatusSegmentsComponent {
  readonly value = model<number | null>(null);
  readonly disabled = input(false);
  readonly eyebrowId = input('task-status-eyebrow');

  readonly eyebrowClass = EYEBROW;

  private readonly statuses = statusResource();

  readonly segments = computed<Status[]>(() => {
    const statuses = this.statuses.value();
    const visible = statuses.slice(0, VISIBLE_SEGMENTS);
    const current = statuses.find((status) => status.id === this.value());

    if (!current || visible.some((status) => status.id === current.id)) {
      return visible;
    }

    return [...visible.slice(0, VISIBLE_SEGMENTS - 1), current];
  });

  readonly overflow = computed(() => {
    const shown = new Set(this.segments().map((status) => status.id));

    return this.statuses.value().filter((status) => !shown.has(status.id));
  });

  readonly overflowClass = computed(() => {
    const selected = this.overflow().some(
      (status) => status.id === this.value()
    );

    return cn(
      'border-foreground/8 text-muted hover:bg-hover hover:text-foreground flex w-9 shrink-0 cursor-pointer items-center justify-center border-l transition-colors',
      selected && 'bg-primary/22'
    );
  });

  segmentClass(selected: boolean, first: boolean) {
    return cn(
      'flex-1 cursor-pointer truncate px-2 text-xs transition-colors disabled:cursor-default',
      !first && 'border-foreground/8 border-l',
      selected
        ? 'bg-primary/22 text-foreground font-semibold'
        : 'text-muted hover:bg-hover hover:text-foreground font-medium'
    );
  }
}
