import {
  booleanAttribute,
  Component,
  computed,
  input,
  output,
} from '@angular/core';
import { LucideLink2, LucideX } from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { ColorSwatchComponent } from '@static/components/color-swatch/color-swatch.component';
import { ListRowComponent } from '@static/components/list-row.component';
import { TaskScopeIdComponent } from '@static/components/task-scope-id.component';
import { SectionLabelDirective } from '@static/directives/section-label.directive';
import { TooltipDirective } from '@static/directives/tooltip.directive';

export interface TaskRelationListItem {
  id: string;
  // How the link reads from this task's end, such as "Blocks" or "Is Blocked By".
  label: string;
  systemId: string;
  name: string;
  statusName: string;
  statusColor?: string | null;
}

interface TaskRelationGroup {
  label: string;
  items: TaskRelationListItem[];
}

// The links a task has, or will have once created, grouped under the label each
// link reads as in this direction.
@Component({
  selector: 'app-task-relation-list',
  imports: [
    ColorSwatchComponent,
    IconButtonComponent,
    ListRowComponent,
    LucideLink2,
    LucideX,
    SectionLabelDirective,
    TaskScopeIdComponent,
    TooltipDirective,
  ],
  host: { class: 'block' },
  template: `
    @for (group of groups(); track group.label) {
      <div class="mb-3">
        <div appSectionLabel class="mb-1">{{ group.label }}</div>

        <ul class="flex flex-col gap-1">
          @for (item of group.items; track item.id) {
            <li app-list-row>
              <app-color-swatch size="sm" [color]="item.statusColor" />

              <app-task-scope-id [id]="item.systemId" />

              @if (openable()) {
                <button
                  type="button"
                  class="flex-1 cursor-pointer truncate text-left"
                  (click)="opened.emit(item)">
                  {{ item.name }}
                </button>
              } @else {
                <span class="flex-1 truncate">{{ item.name }}</span>
              }

              <span class="text-muted shrink-0 text-xs">
                {{ item.statusName }}
              </span>

              @if (removable()) {
                <button
                  app-icon-button
                  type="button"
                  i18n-appTooltip="
                    Tooltip on the button that removes a task link
                  "
                  appTooltip="Remove link"
                  i18n-aria-label="
                    Accessible label for the button that removes a task link
                  "
                  aria-label="Remove link"
                  [disabled]="disabled()"
                  (click)="removed.emit(item)">
                  <svg lucideX class="h-4 w-4"></svg>
                </button>
              }
            </li>
          }
        </ul>
      </div>
    } @empty {
      <div class="text-muted flex items-center gap-2 p-4 text-sm">
        <svg lucideLink2 class="h-4 w-4"></svg>
        <span i18n="Empty state when a task has no links to other tasks">
          No linked tasks
        </span>
      </div>
    }
  `,
})
export class TaskRelationListComponent {
  readonly items = input.required<readonly TaskRelationListItem[]>();
  readonly removable = input(false, { transform: booleanAttribute });
  readonly openable = input(false, { transform: booleanAttribute });
  readonly disabled = input(false, { transform: booleanAttribute });

  readonly removed = output<TaskRelationListItem>();
  readonly opened = output<TaskRelationListItem>();

  // Grouped by label rather than by relation type: one type produces two groups
  // depending on which end this task sits on.
  protected readonly groups = computed<TaskRelationGroup[]>(() => {
    const groups: TaskRelationGroup[] = [];

    for (const item of this.items()) {
      const existing = groups.find((group) => group.label === item.label);

      if (existing) {
        existing.items.push(item);
        continue;
      }

      groups.push({ label: item.label, items: [item] });
    }

    return groups;
  });
}
