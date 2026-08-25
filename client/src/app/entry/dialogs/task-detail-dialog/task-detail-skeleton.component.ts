import { Component } from '@angular/core';
import { SkeletonComponent } from '@static/components/skeleton/skeleton.component';

// Holds the height the loaded dialog settles at, so opening a task does not resize the dialog
// once the request comes back.
@Component({
  selector: 'app-task-detail-skeleton',
  imports: [SkeletonComponent],
  host: {
    class: 'block h-243.5',
    role: 'status',
    '[attr.aria-label]': 'label',
  },
  template: `
    <div
      class="mb-1 flex flex-row items-center justify-between gap-4 pr-6 pl-2">
      <app-skeleton class="ml-2 h-8 w-96" />

      <div class="flex items-center gap-4">
        <app-skeleton class="h-6 w-24 rounded-full" />
        <app-skeleton class="h-5 w-16" />
        <app-skeleton class="h-8 w-28 rounded-md" />
        <app-skeleton class="h-8 w-8 rounded-md" />
      </div>
    </div>

    <div class="flex flex-row gap-12 px-6">
      <div class="flex w-64 grow flex-col">
        <app-skeleton class="mt-4 mb-2 h-4 w-12" />
        <app-skeleton class="h-9 w-full" />

        <app-skeleton class="mt-6 mb-2 h-4 w-24" />
        <app-skeleton class="h-52 w-full" />

        <app-skeleton class="mt-6 mb-2 h-4 w-14" />
        <app-skeleton class="h-10 w-1/2" />

        <app-skeleton class="mt-6 mb-2 h-4 w-20" />
        <div class="flex flex-col gap-2">
          <app-skeleton class="h-10 w-full" />
          <app-skeleton class="h-10 w-4/5" />
        </div>

        <app-skeleton class="mt-6 mb-2 h-4 w-16" />
        <app-skeleton class="h-10 w-2/3" />

        <app-skeleton class="mt-6 mb-2 h-4 w-24" />
        <div class="flex flex-col gap-3">
          @for (comment of commentRange; track $index) {
            <div class="flex flex-row gap-3">
              <app-skeleton class="h-8 w-8 shrink-0 rounded-full" />
              <div class="flex flex-1 flex-col gap-2">
                <app-skeleton class="h-3 w-32" />
                <app-skeleton class="h-3 w-3/4" />
              </div>
            </div>
          }

          <app-skeleton class="mt-3 h-20 w-full" />
        </div>
      </div>

      <div class="bg-card/40 mt-4 flex w-56 flex-col rounded px-6 pb-6">
        @for (field of fieldRange; track $index) {
          <app-skeleton class="mt-4 mb-2 h-4 w-20" />
          <app-skeleton class="h-9 w-full" />
        }
      </div>
    </div>
  `,
})
export class TaskDetailSkeletonComponent {
  readonly label = $localize`:Accessible label shown while a task loads:Loading task`;
  readonly fieldRange = Array.from({ length: 8 });
  readonly commentRange = Array.from({ length: 3 });
}
