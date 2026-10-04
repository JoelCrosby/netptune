import { Component, computed, input } from '@angular/core';
import { SkeletonComponent } from './skeleton.component';

// Stands in for grouped `app-entity-list`s while they load.
@Component({
  selector: 'app-skeleton-entity-list',
  imports: [SkeletonComponent],
  host: {
    class: 'flex flex-col gap-6',
    role: 'status',
    '[attr.aria-label]': 'label()',
  },
  template: `
    @for (group of groupRange(); track $index) {
      <div class="flex flex-col gap-3">
        <div class="flex items-center gap-2.5 py-1">
          <app-skeleton class="h-4 w-4" />
          <app-skeleton class="h-4 w-36" />
          <app-skeleton class="h-5 w-6 rounded-full" />
          <span class="bg-border h-px flex-1"></span>
        </div>

        <div class="border-border overflow-hidden rounded border shadow-sm">
          <div class="bg-card-header border-border h-10.5 border-b"></div>

          @for (row of rowRange(); track $index) {
            <div
              class="bg-card border-border flex min-h-15 items-center gap-3 border-b px-4 last:border-b-0">
              <app-skeleton class="h-8 w-8 rounded-lg" />
              <div class="flex flex-1 flex-col gap-1.5">
                <app-skeleton class="h-3.5 w-48 max-w-full" />
                <app-skeleton class="h-2.5 w-24" />
              </div>
              <app-skeleton class="h-6 w-20 rounded-full max-md:hidden" />
              <app-skeleton class="h-3.5 w-8" />
              <app-skeleton class="h-3 w-24 max-md:hidden" />
            </div>
          }
        </div>
      </div>
    }
  `,
})
export class SkeletonEntityListComponent {
  readonly groups = input(2);
  readonly rows = input(3);
  readonly label = input(
    $localize`:Accessible label while a grouped list loads:Loading`
  );

  protected readonly groupRange = computed(() => {
    return Array.from({ length: this.groups() });
  });

  protected readonly rowRange = computed(() => {
    return Array.from({ length: this.rows() });
  });
}
