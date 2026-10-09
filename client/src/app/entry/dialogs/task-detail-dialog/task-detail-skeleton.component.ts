import { Component, computed, inject } from '@angular/core';
import { SkeletonComponent } from '@static/components/skeleton/skeleton.component';
import { TaskDetailLayoutService } from './task-detail-layout';
import { DialogColumnsComponent } from '@static/components/dialog/dialog-columns.component';
import { DialogRailComponent } from '@static/components/dialog/dialog-rail.component';
import { DialogSectionComponent } from '@static/components/dialog/dialog-section.component';

@Component({
  selector: 'app-task-detail-skeleton',
  imports: [
    DialogColumnsComponent,
    DialogRailComponent,
    DialogSectionComponent,
    SkeletonComponent,
  ],
  host: {
    class: 'flex h-full min-h-0 flex-col',
    role: 'status',
    '[attr.aria-label]': 'label',
  },
  template: `
    @if (isDocument()) {
      <div
        class="border-foreground/8 flex h-12 shrink-0 items-center gap-2.5 border-b pr-3.5 pl-5">
        <app-skeleton class="h-6 w-20 rounded-sm" />
        <div class="ml-auto flex items-center gap-2">
          <app-skeleton class="h-8 w-20 rounded-lg" />
          <app-skeleton class="h-8 w-8 rounded-md" />
        </div>
      </div>

      <div class="flex min-h-0 flex-1 justify-center overflow-hidden pt-2">
        <div class="flex w-[680px] max-w-full flex-col gap-[22px] px-4 pb-7">
          <app-skeleton class="h-10 w-3/4" />

          <div class="flex flex-wrap items-center gap-2">
            <app-skeleton class="h-[26px] w-28 rounded-md" />
            <app-skeleton class="h-[26px] w-32 rounded-md" />
            <app-skeleton class="h-[26px] w-24 rounded-md" />
          </div>

          <div class="flex gap-1.5">
            <app-skeleton class="h-[26px] w-24 rounded-md" />
            <app-skeleton class="h-[26px] w-[26px] rounded-md" />
          </div>

          <div class="flex flex-col gap-2.5">
            @for (line of bodyRange; track $index) {
              <app-skeleton class="h-4" [class]="lineWidth($index)" />
            }
          </div>

          <div class="bg-foreground/8 h-px" aria-hidden="true"></div>

          <div class="flex items-center gap-4.5">
            @for (tab of tabRange; track $index) {
              <app-skeleton class="h-4 w-16" />
            }
          </div>
        </div>
      </div>

      <div
        class="border-foreground/8 flex shrink-0 justify-center border-t py-3">
        <div class="w-[680px] max-w-full px-4">
          <app-skeleton class="h-9 w-full rounded-full" />
        </div>
      </div>
    } @else {
      <div
        class="border-foreground/8 flex h-[52px] shrink-0 items-center gap-2.5 border-b pr-3.5 pl-5">
        <app-skeleton class="h-6 w-20 rounded-sm" />
        <app-skeleton class="h-4 w-40" />
        <div class="ml-auto flex items-center gap-2">
          <app-skeleton class="h-8 w-24 rounded-lg" />
          <app-skeleton class="h-8 w-8 rounded-md" />
          <app-skeleton class="h-8 w-8 rounded-md" />
        </div>
      </div>

      <div
        class="border-foreground/8 flex shrink-0 flex-col gap-3.5 border-b px-7 pt-5.5 pb-4">
        <app-skeleton class="h-9 w-3/4" />

        <div class="flex flex-wrap items-center gap-2">
          @for (chip of chipRange; track $index) {
            <app-skeleton class="h-[26px] w-24 rounded-md" />
          }
          <app-skeleton class="h-[26px] w-[26px] rounded-md" />
        </div>
      </div>

      <app-dialog-columns bodyClass="px-7 pt-5.5 pb-6">
        <div class="flex flex-col gap-2.5">
          @for (line of bodyRange; track $index) {
            <app-skeleton class="h-4" [class]="lineWidth($index)" />
          }
        </div>

        <app-dialog-section dialogColumnsFooter divider="top" class="shrink-0">
          <div class="flex h-[52px] items-center gap-2 px-5">
            @for (tab of footerTabRange; track $index) {
              <app-skeleton class="h-7 w-20 rounded-md" />
            }
          </div>
        </app-dialog-section>

        <app-dialog-rail class="w-[372px]">
          <div
            class="border-foreground/8 flex h-[52px] shrink-0 items-center gap-2 border-b px-4">
            <app-skeleton class="h-7 w-24 rounded-md" />
            <app-skeleton class="h-7 w-20 rounded-md" />
          </div>

          <div class="flex min-h-0 flex-1 flex-col gap-4 px-4 py-3.5">
            @for (comment of commentRange; track $index) {
              <div class="flex gap-2.5">
                <app-skeleton class="h-7 w-7 shrink-0 rounded-full" />
                <div class="flex flex-1 flex-col gap-1.5">
                  <app-skeleton class="h-3.5 w-24" />
                  <app-skeleton class="h-3.5 w-full" />
                  <app-skeleton class="h-3.5 w-2/3" />
                </div>
              </div>
            }
          </div>

          <div class="border-foreground/8 shrink-0 border-t px-4 py-3">
            <app-skeleton class="h-9 w-full rounded-lg" />
          </div>
        </app-dialog-rail>
      </app-dialog-columns>

      <div
        class="border-foreground/8 flex h-10 shrink-0 items-center gap-4 border-t px-5">
        <app-skeleton class="h-3 w-32" />
        <app-skeleton class="h-3 w-28" />
      </div>
    }
  `,
})
export class TaskDetailSkeletonComponent {
  private readonly layout = inject(TaskDetailLayoutService);

  protected readonly isDocument = computed(() => {
    return this.layout.layout() === 'document';
  });

  readonly label = $localize`:Accessible label shown while a task loads:Loading task`;
  readonly chipRange = Array.from({ length: 4 });
  readonly bodyRange = Array.from({ length: 6 });
  readonly footerTabRange = Array.from({ length: 3 });
  readonly commentRange = Array.from({ length: 3 });
  readonly tabRange = Array.from({ length: 4 });

  lineWidth(index: number) {
    return ['w-full', 'w-11/12', 'w-full', 'w-4/5', 'w-full', 'w-2/3'][index];
  }
}
