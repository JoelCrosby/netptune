import {
  booleanAttribute,
  Component,
  computed,
  inject,
  input,
  output,
} from '@angular/core';
import { cn } from '../button/button.variants';
import { ResizeHandleComponent } from '../resize-handle.component';
import { DialogColumnsComponent } from './dialog-columns.component';

const DEFAULT_RAIL_WIDTH = 340;
const MAX_COLUMNS_SHARE = 0.5;

// The tinted side column of a two-column dialog. Below 1200px the dialog stacks,
// so the rail drops underneath the main column and runs full width.
@Component({
  selector: 'app-dialog-rail, [app-dialog-rail]',
  imports: [ResizeHandleComponent],
  host: {
    '[class]': 'hostClass()',
    '[style.--dialog-rail-width]': 'widthValue()',
  },
  template: `
    @if (resizable()) {
      <app-resize-handle
        class="max-[1200px]:hidden"
        [label]="resizeLabel()"
        [width]="currentWidth()"
        [minWidth]="bounds().min"
        [maxWidth]="bounds().max"
        (widthChange)="onResize($event)"
        (resizingChange)="resizingChange.emit($event)" />
    }

    <ng-content />
  `,
})
export class DialogRailComponent {
  private readonly columns = inject(DialogColumnsComponent, { optional: true });

  readonly collapsed = input(false, { transform: booleanAttribute });
  readonly resizable = input(false, { transform: booleanAttribute });
  readonly width = input<number | null>(null);
  readonly minWidth = input(280);
  readonly maxWidth = input(560);
  readonly resizeLabel = input('');
  readonly class = input('');

  readonly widthChange = output<number>();
  readonly resizingChange = output<boolean>();

  protected readonly bounds = computed(() => {
    const columnsWidth = this.columns?.width() ?? null;
    const cap =
      columnsWidth === null
        ? Infinity
        : Math.round(columnsWidth * MAX_COLUMNS_SHARE);
    const max = Math.min(this.maxWidth(), cap);

    return { min: Math.min(this.minWidth(), max), max };
  });

  protected readonly currentWidth = computed(() => {
    return clamp(this.width() ?? DEFAULT_RAIL_WIDTH, this.bounds());
  });

  protected readonly widthValue = computed(() => {
    return this.width() === null ? null : `${this.currentWidth()}px`;
  });

  protected readonly hostClass = computed(() => {
    return cn(
      'border-foreground/8 bg-foreground/2 relative flex w-[var(--dialog-rail-width,340px)] max-w-1/2 shrink-0 flex-col border-l max-[1200px]:w-full max-[1200px]:max-w-none max-[1200px]:border-t max-[1200px]:border-l-0',
      this.class(),
      this.collapsed() && 'hidden'
    );
  });

  protected onResize(width: number) {
    this.widthChange.emit(clamp(width, this.bounds()));
  }
}

function clamp(width: number, bounds: { min: number; max: number }): number {
  return Math.min(bounds.max, Math.max(bounds.min, width));
}
