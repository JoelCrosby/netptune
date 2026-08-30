import { Component } from '@angular/core';
import { SkeletonComponent } from '@static/components/skeleton/skeleton.component';

@Component({
  selector: 'app-task-detail-skeleton',
  imports: [SkeletonComponent],
  host: {
    class: 'flex h-full min-h-0 flex-col',
    role: 'status',
    '[attr.aria-label]': 'label',
  },
  template: `
    <div
      class="border-foreground/8 flex h-[50px] shrink-0 items-center gap-2.5 border-b pr-3.5 pl-5">
      <app-skeleton class="h-6 w-20 rounded-sm" />
      <app-skeleton class="h-4 w-40" />
      <div class="ml-auto flex items-center gap-2">
        <app-skeleton class="h-8 w-24 rounded-lg" />
        <app-skeleton class="h-8 w-8 rounded-md" />
        <app-skeleton class="h-8 w-8 rounded-md" />
      </div>
    </div>

    <div class="flex min-h-0 flex-1 flex-row">
      <div class="flex min-w-0 flex-1 flex-col gap-[18px] px-7 pt-6">
        <app-skeleton class="h-9 w-3/4" />

        <div class="flex gap-1.5">
          <app-skeleton class="h-[26px] w-28 rounded-md" />
          <app-skeleton class="h-[26px] w-24 rounded-md" />
          <app-skeleton class="h-[26px] w-[26px] rounded-md" />
        </div>

        <div class="flex flex-col gap-2.5">
          @for (line of bodyRange; track $index) {
            <app-skeleton class="h-4" [class]="lineWidth($index)" />
          }
        </div>

        <div class="border-foreground/8 mt-auto flex flex-col border-t">
          @for (row of sectionRange; track $index) {
            <div
              class="border-foreground/8 flex h-[46px] items-center gap-3 border-b last:border-b-0">
              <app-skeleton class="h-3.5 w-3.5 rounded-sm" />
              <app-skeleton class="h-4 w-20" />
              <app-skeleton class="h-3 w-32" />
            </div>
          }
        </div>
      </div>

      <div
        class="border-foreground/8 bg-foreground/[0.02] flex w-[340px] shrink-0 flex-col border-l">
        <div
          class="border-foreground/8 flex flex-col gap-2.5 border-b px-5 pt-4.5 pb-4">
          <app-skeleton class="h-2.5 w-12" />
          <app-skeleton class="h-8 w-full rounded-lg" />
        </div>

        <div class="flex flex-col gap-1 px-2 pt-2">
          @for (field of fieldRange; track $index) {
            <div class="flex h-10 items-center gap-3 px-3">
              <app-skeleton class="h-3.5 w-16" />
              <app-skeleton class="h-3.5 w-24" />
            </div>
          }
        </div>
      </div>
    </div>
  `,
})
export class TaskDetailSkeletonComponent {
  readonly label = $localize`:Accessible label shown while a task loads:Loading task`;
  readonly fieldRange = Array.from({ length: 5 });
  readonly bodyRange = Array.from({ length: 6 });
  readonly sectionRange = Array.from({ length: 3 });

  lineWidth(index: number) {
    return ['w-full', 'w-11/12', 'w-full', 'w-4/5', 'w-full', 'w-2/3'][index];
  }
}
