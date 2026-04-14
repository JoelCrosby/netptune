import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
} from '@angular/core';
import {
  netptunePermissionLabels,
  PermissionMeta,
} from '@core/auth/permission-items';
import { LucideDynamicIcon } from '@lucide/angular';

interface PermissionGroup {
  heading: string;
  items: PermissionMeta[];
}

@Component({
  selector: 'app-permission-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideDynamicIcon],
  host: {
    class:
      'block w-full bg-board-group overflow-auto max-h-[calc(100vh-600px)] custom-scroll rounded border-border/60 border',
  },
  template: `
    <div class="flex w-full flex-col">
      @for (group of groups(); track group.heading) {
        <div class="w-full">
          <h4
            class="border-border text-foreground/80 bg-background sticky top-0 mb-2 border-b px-4 pt-8 pb-4 tracking-wide capitalize">
            {{ group.heading }}
          </h4>
          <div class="flex w-full flex-col">
            @for (item of group.items; track item.key) {
              <div
                class="border-border flex h-14 w-full items-center gap-3 border-b px-4">
                <svg
                  [lucideIcon]="item.icon"
                  class="text-foreground/40 h-6 w-6 shrink-0"></svg>
                <span class="text-sm">{{ item.label }}</span>
              </div>
            }
          </div>
        </div>
      }
    </div>
  `,
})
export class PermissionListComponent {
  readonly permissions = input.required<string[]>();

  readonly groups = computed<PermissionGroup[]>(() => {
    const permSet = new Set(this.permissions());
    const groups: PermissionGroup[] = [];

    for (const [groupKey, groupValue] of Object.entries(
      netptunePermissionLabels
    )) {
      const items = Object.values(
        groupValue as Record<string, PermissionMeta>
      ).filter((meta) => permSet.has(meta.key));

      if (items.length > 0) {
        groups.push({
          heading: groupKey.replace(/([A-Z])/g, ' $1').trim(),
          items,
        });
      }
    }

    return groups;
  });
}
