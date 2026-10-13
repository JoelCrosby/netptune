import { NgTemplateOutlet } from '@angular/common';
import {
  Component,
  computed,
  contentChild,
  input,
  TemplateRef,
} from '@angular/core';
import { cn } from '../button/button.variants';
import { DiffMode, DiffOpKind } from '../text-diff/text-diff';

export interface BlockDiffItem<T> {
  kind: DiffOpKind;
  value: T;
}

const BLOCK_CLASSES: Record<DiffOpKind, string> = {
  added: 'bg-diff-add border-change-added',
  removed: 'bg-diff-del border-change-removed text-muted',
  context: 'border-transparent',
};

@Component({
  selector: 'app-block-diff',
  imports: [NgTemplateOutlet],
  host: { '[class]': 'hostClass()' },
  template: `
    @if (mode() === 'split') {
      <div class="grid grid-cols-[minmax(0,1fr)_1px_minmax(0,1fr)]">
        <div class="min-w-0">
          <div
            class="border-border text-muted border-b px-3 py-2 text-xs tracking-wide uppercase">
            {{ beforeLabel() }}
          </div>
          <ng-container
            *ngTemplateOutlet="column; context: { $implicit: beforeItems() }" />
        </div>

        <div class="bg-border"></div>

        <div class="min-w-0">
          <div
            class="border-border text-muted border-b px-3 py-2 text-xs tracking-wide uppercase">
            {{ afterLabel() }}
          </div>
          <ng-container
            *ngTemplateOutlet="column; context: { $implicit: afterItems() }" />
        </div>
      </div>
    } @else {
      <ng-container
        *ngTemplateOutlet="column; context: { $implicit: items() }" />
    }

    <ng-template #column let-list>
      <div class="flex flex-col gap-1 py-2">
        @for (item of list; track $index) {
          <div class="border-l-2 px-3 py-1" [class]="blockClass(item.kind)">
            <ng-container
              *ngTemplateOutlet="block(); context: { $implicit: item.value }" />
          </div>
        } @empty {
          <span class="text-muted px-3 py-1 text-sm italic">{{
            emptyLabel()
          }}</span>
        }
      </div>
    </ng-template>
  `,
})
export class BlockDiffComponent<T> {
  readonly items = input.required<BlockDiffItem<T>[]>();
  readonly mode = input<DiffMode>('split');
  readonly beforeLabel = input(
    $localize`:Column heading above the earlier text in a side by side comparison:Before`
  );
  readonly afterLabel = input(
    $localize`:Column heading above the later text in a side by side comparison:After`
  );
  readonly emptyLabel = input(
    $localize`:Shown on one side of a diff where the value is empty:Empty`
  );
  readonly class = input('');

  protected readonly block = contentChild.required(TemplateRef);

  protected readonly beforeItems = computed(() => {
    return this.items().filter((item) => item.kind !== 'added');
  });

  protected readonly afterItems = computed(() => {
    return this.items().filter((item) => item.kind !== 'removed');
  });

  protected readonly hostClass = computed(() => cn('block', this.class()));

  protected blockClass(kind: DiffOpKind): string {
    return BLOCK_CLASSES[kind];
  }
}
