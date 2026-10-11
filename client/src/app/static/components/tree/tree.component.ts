import {
  booleanAttribute,
  Component,
  computed,
  ElementRef,
  inject,
  input,
  model,
  output,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  LucideChevronRight,
  LucideDynamicIcon,
  LucidePlus,
} from '@lucide/angular';
import { cn } from '../button/button.variants';
import { buildTreeRows, collectDescendantIds } from './tree-rows';
import { TreeDropPosition, TreeMove, TreeNode, TreeRow } from './tree.types';

const indentPerLevelPx = 14;
const baseIndentPx = 4;
const flatIndentPx = 8;

export type TreeSize = 'default' | 'large';
const edgeDropZone = 0.25;

interface DropTarget {
  id: number;
  position: TreeDropPosition;
}

@Component({
  selector: 'app-tree',
  imports: [RouterLink, LucideChevronRight, LucideDynamicIcon, LucidePlus],
  host: { '[class]': 'hostClass()' },
  template: `
    <ul
      role="tree"
      class="flex flex-col gap-px"
      [attr.aria-label]="ariaLabel()">
      @for (row of rows(); track row.node.id; let index = $index) {
        <li
          role="treeitem"
          [attr.aria-level]="row.depth + 1"
          [attr.aria-expanded]="row.childCount ? row.expanded : null"
          [attr.aria-selected]="row.node.id === selectedId()"
          [class]="rowClass(row)"
          [style.padding-left.px]="indent(row)"
          (dragover)="onDragOver($event, row)"
          (dragleave)="onDragLeave(row)"
          (drop)="onDrop($event, row)">
          @if (isNested()) {
            <button
              type="button"
              tabindex="-1"
              class="text-foreground/45 hover:text-foreground flex h-5 w-5 shrink-0 items-center justify-center rounded"
              [class.invisible]="!row.childCount"
              [attr.aria-label]="row.expanded ? collapseLabel : expandLabel"
              (click)="onToggle(row)">
              <svg
                lucideChevronRight
                class="h-3 w-3 transition-transform duration-150"
                [class.rotate-90]="row.expanded"></svg>
            </button>
          }

          <a
            data-tree-link
            class="flex h-full min-w-0 flex-1 items-center outline-none"
            [class]="isLarge() ? 'gap-3' : 'gap-2'"
            [routerLink]="row.node.link"
            [queryParams]="row.node.queryParams ?? null"
            [draggable]="draggable()"
            [attr.tabindex]="index === focusIndex() ? 0 : -1"
            (focus)="focusIndex.set(index)"
            (keydown)="onKeydown($event, row, index)"
            (dragstart)="onDragStart($event, row)"
            (dragend)="onDragEnd()">
            <svg
              [lucideIcon]="row.node.icon"
              class="shrink-0"
              [class]="iconClass(row)"></svg>
            <span class="min-w-0 flex-1 truncate">{{ row.node.label }}</span>
            @if (row.childCount && !row.expanded) {
              <span class="text-foreground/40 text-[11.5px] tabular-nums">
                {{ row.childCount }}
              </span>
            }
          </a>

          @if (addLabel(); as label) {
            <button
              type="button"
              tabindex="-1"
              class="text-foreground/50 hover:bg-foreground/10 hover:text-foreground flex h-5 w-5 shrink-0 items-center justify-center rounded opacity-0 group-hover:opacity-100 focus-visible:opacity-100"
              [attr.aria-label]="label"
              [attr.title]="label"
              (click)="add.emit(row.node.id)">
              <svg lucidePlus class="h-3.5 w-3.5"></svg>
            </button>
          }
        </li>
      }
    </ul>
  `,
})
export class TreeComponent {
  readonly class = input('');

  protected readonly hostClass = computed(() => cn('block', this.class()));

  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly nodes = input.required<readonly TreeNode[]>();
  readonly selectedId = input<number | null>(null);
  readonly expanded = model<readonly number[]>([]);
  readonly draggable = input(false, { transform: booleanAttribute });
  readonly ariaLabel = input<string | null>(null);
  readonly addLabel = input<string | null>(null);
  readonly size = input<TreeSize>('default');

  readonly move = output<TreeMove>();
  readonly add = output<number>();

  protected readonly expandLabel = $localize`:Accessible label for the button that shows a tree item's children:Expand`;
  protected readonly collapseLabel = $localize`:Accessible label for the button that hides a tree item's children:Collapse`;

  protected readonly isLarge = computed(() => this.size() === 'large');

  protected readonly focusIndex = signal(0);
  private readonly draggingId = signal<number | null>(null);
  private readonly dropTarget = signal<DropTarget | null>(null);

  protected readonly rows = computed(() => {
    const expandedIds = new Set(this.expanded());

    return buildTreeRows(this.nodes(), expandedIds);
  });

  protected readonly isNested = computed(() => {
    return this.nodes().some((node) => node.parentId !== null);
  });

  protected indent(row: TreeRow): number {
    if (!this.isNested()) return flatIndentPx;

    return baseIndentPx + row.depth * indentPerLevelPx;
  }

  protected rowClass(row: TreeRow): string {
    const selected = row.node.id === this.selectedId();
    const target = this.dropTarget();
    const isTarget = target?.id === row.node.id;

    return cn(
      'group text-foreground/75 hover:bg-foreground/5 relative flex items-center gap-1 rounded-md pr-1.5 text-sm',
      this.isLarge() ? 'h-9' : 'h-8',
      selected &&
        'bg-primary/10 text-primary hover:bg-primary/15 font-semibold',
      isTarget &&
        target.position === 'inside' &&
        'ring-primary/60 bg-primary/10 ring-1',
      isTarget &&
        target.position === 'before' &&
        'shadow-[inset_0_2px_0_var(--primary)]',
      isTarget &&
        target.position === 'after' &&
        'shadow-[inset_0_-2px_0_var(--primary)]',
      this.draggingId() === row.node.id && 'opacity-50'
    );
  }

  protected iconClass(row: TreeRow): string {
    const selected = row.node.id === this.selectedId();

    const size = this.isLarge() ? 'h-5 w-5' : 'h-4 w-4';
    const colour = selected ? 'text-primary' : 'text-foreground/50';

    return `${size} ${colour}`;
  }

  protected onToggle(row: TreeRow) {
    this.setExpanded(row.node.id, !row.expanded);
  }

  protected onKeydown(event: KeyboardEvent, row: TreeRow, index: number) {
    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.focusRow(index + 1);
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.focusRow(index - 1);
        break;
      case 'ArrowRight':
        event.preventDefault();
        this.onArrowRight(row, index);
        break;
      case 'ArrowLeft':
        event.preventDefault();
        this.onArrowLeft(row);
        break;
      case 'Home':
        event.preventDefault();
        this.focusRow(0);
        break;
      case 'End':
        event.preventDefault();
        this.focusRow(this.rows().length - 1);
        break;
    }
  }

  protected onDragStart(event: DragEvent, row: TreeRow) {
    if (!this.draggable()) return;

    this.draggingId.set(row.node.id);
    event.dataTransfer?.setData('text/plain', row.node.label);

    if (event.dataTransfer) {
      event.dataTransfer.effectAllowed = 'move';
    }
  }

  protected onDragOver(event: DragEvent, row: TreeRow) {
    const draggingId = this.draggingId();

    if (draggingId === null) return;

    const isValidTarget = this.canDropOn(draggingId, row.node.id);

    if (!isValidTarget) return;

    event.preventDefault();

    const position = dropPositionFor(event);

    this.dropTarget.set({ id: row.node.id, position });
  }

  protected onDragLeave(row: TreeRow) {
    const leavingTarget = this.dropTarget()?.id === row.node.id;

    if (leavingTarget) {
      this.dropTarget.set(null);
    }
  }

  protected onDrop(event: DragEvent, row: TreeRow) {
    const draggingId = this.draggingId();
    const target = this.dropTarget();

    event.preventDefault();
    this.onDragEnd();

    const isDropOnRow = target?.id === row.node.id;

    if (draggingId === null || !target || !isDropOnRow) return;

    if (target.position === 'inside') {
      this.setExpanded(row.node.id, true);
    }

    this.move.emit({
      nodeId: draggingId,
      targetId: row.node.id,
      position: target.position,
    });
  }

  protected onDragEnd() {
    this.draggingId.set(null);
    this.dropTarget.set(null);
  }

  private onArrowRight(row: TreeRow, index: number) {
    if (!row.childCount) return;

    if (!row.expanded) {
      this.setExpanded(row.node.id, true);

      return;
    }

    this.focusRow(index + 1);
  }

  private onArrowLeft(row: TreeRow) {
    if (row.childCount && row.expanded) {
      this.setExpanded(row.node.id, false);

      return;
    }

    const parentIndex = this.rows().findIndex((candidate) => {
      return candidate.node.id === row.node.parentId;
    });

    if (parentIndex >= 0) {
      this.focusRow(parentIndex);
    }
  }

  private canDropOn(draggingId: number, targetId: number): boolean {
    if (draggingId === targetId) return false;

    const descendants = collectDescendantIds(this.nodes(), draggingId);

    return !descendants.has(targetId);
  }

  private setExpanded(id: number, expanded: boolean) {
    const current = this.expanded().filter((expandedId) => expandedId !== id);

    this.expanded.set(expanded ? [...current, id] : current);
  }

  private focusRow(index: number) {
    const links =
      this.host.nativeElement.querySelectorAll<HTMLElement>('[data-tree-link]');
    const clamped = Math.max(0, Math.min(index, links.length - 1));

    this.focusIndex.set(clamped);
    links.item(clamped)?.focus();
  }
}

function dropPositionFor(event: DragEvent): TreeDropPosition {
  const row = event.currentTarget as HTMLElement;
  const bounds = row.getBoundingClientRect();
  const offset = (event.clientY - bounds.top) / bounds.height;

  if (offset < edgeDropZone) return 'before';

  if (offset > 1 - edgeDropZone) return 'after';

  return 'inside';
}
