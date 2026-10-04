import { Component, input, output } from '@angular/core';
import { LucidePlus } from '@lucide/angular';
import { EntityListRowComponent } from './entity-list-row.component';
import { entityListColumnsClass } from './entity-list.columns';
import { EntityListItem } from './entity-list.types';

@Component({
  selector: 'app-entity-list',
  imports: [EntityListRowComponent, LucidePlus],
  host: { class: 'block' },
  template: `
    <div class="border-border overflow-hidden rounded border shadow-sm">
      <div
        [class]="headerClass"
        class="bg-card-header text-muted border-border border-b py-3 text-xs font-medium tracking-wide uppercase"
        aria-hidden="true">
        <span>{{ nameHeading() }}</span>
        <span class="max-md:hidden">{{ peopleHeading() }}</span>
        <span class="text-right">{{ countHeading() }}</span>
        <span class="max-md:hidden">{{ updatedHeading() }}</span>
        <span></span>
      </div>

      <div role="list">
        @for (item of items(); track item.id) {
          <app-entity-list-row role="listitem" [item]="item" />
        }
      </div>

      @if (actionLabel(); as label) {
        <button
          type="button"
          class="bg-card hover:bg-card-hover border-border text-muted hover:text-primary focus-visible:text-primary flex h-11 w-full cursor-pointer items-center gap-2.5 border-t px-4 text-sm font-medium transition-colors outline-none"
          (click)="actionClick.emit()">
          <svg lucidePlus class="h-3.75 w-3.75" aria-hidden="true"></svg>
          <span>{{ label }}</span>
        </button>
      }
    </div>
  `,
})
export class EntityListComponent {
  readonly items = input.required<readonly EntityListItem[]>();
  readonly nameHeading = input.required<string>();
  readonly countHeading = input.required<string>();
  readonly peopleHeading = input(
    $localize`:Column heading for the people involved with a list item:People`
  );
  readonly updatedHeading = input(
    $localize`:Column heading for when a list item was last changed:Modified`
  );
  readonly actionLabel = input<string | null>(null);

  readonly actionClick = output();

  protected readonly headerClass = entityListColumnsClass;
}
