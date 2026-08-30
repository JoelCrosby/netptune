import { Component, input } from '@angular/core';

@Component({
  selector: 'app-task-detail-layout-preview',
  host: { class: 'block' },
  template: `
    <span
      class="border-foreground/8 bg-foreground/2 flex h-[116px] overflow-hidden rounded-md border"
      [class.flex-col]="layout() !== 'summary-rail'"
      [class.items-center]="layout() === 'document'"
      [class.py-2]="layout() === 'document'"
      aria-hidden="true">
      @switch (layout()) {
        @case ('cockpit') {
          <span
            class="border-foreground/8 flex flex-col gap-[5px] border-b px-2 pt-2 pb-1.5">
            <span class="bg-foreground/50 h-2 w-[62%] rounded-[3px]"></span>
            <span class="flex gap-1">
              <span class="bg-primary h-2 w-[30px] rounded-[4px]"></span>
              <span
                class="border-foreground/8 h-2 w-[34px] rounded-[4px] border"></span>
              <span
                class="border-foreground/8 h-2 w-[26px] rounded-[4px] border"></span>
              <span
                class="border-foreground/8 h-2 w-[22px] rounded-[4px] border border-dashed"></span>
            </span>
          </span>

          <span class="flex min-h-0 flex-1">
            <span class="flex flex-1 flex-col gap-1 p-[7px]">
              <span class="bg-foreground/12 h-1 rounded-[2px]"></span>
              <span class="bg-foreground/12 h-1 w-[88%] rounded-[2px]"></span>
              <span class="bg-foreground/12 h-1 w-[70%] rounded-[2px]"></span>
              <span class="flex-1"></span>
              <span class="flex gap-1">
                <span
                  class="bg-foreground/30 h-[5px] w-[18px] rounded-[2px]"></span>
                <span
                  class="bg-foreground/14 h-[5px] w-3.5 rounded-[2px]"></span>
              </span>
            </span>

            <span
              class="border-foreground/8 bg-hover flex w-[84px] flex-col gap-[5px] border-l px-1.5 py-[7px]">
              @for (row of commentRows; track $index) {
                <span class="flex items-center gap-1">
                  <span
                    class="bg-foreground/25 h-[9px] w-[9px] rounded-full"></span>
                  <span
                    class="bg-foreground/16 h-1 flex-1 rounded-[2px]"></span>
                </span>
              }
              <span class="bg-foreground/10 h-1 w-[70%] rounded-[2px]"></span>
              <span class="flex-1"></span>
              <span
                class="border-foreground/8 h-[11px] rounded-md border"></span>
            </span>
          </span>
        }

        @case ('document') {
          <span class="flex w-[62%] flex-col gap-[5px]">
            <span class="bg-foreground/50 h-[9px] w-full rounded-[3px]"></span>
            <span class="flex items-center gap-[3px]">
              <span class="bg-primary h-1.5 w-5 rounded-[3px]"></span>
              <span class="bg-foreground/16 h-1.5 w-6 rounded-[3px]"></span>
              <span class="bg-foreground/16 h-1.5 w-4 rounded-[3px]"></span>
            </span>
            <span class="bg-foreground/12 h-1 rounded-[2px]"></span>
            <span class="bg-foreground/12 h-1 rounded-[2px]"></span>
            <span class="bg-foreground/12 h-1 w-[84%] rounded-[2px]"></span>
            <span class="bg-foreground/12 h-1 w-[92%] rounded-[2px]"></span>
            <span class="bg-foreground/8 my-0.5 h-px"></span>
            <span class="flex gap-1">
              <span class="bg-foreground/30 h-[5px] w-5 rounded-[2px]"></span>
              <span class="bg-foreground/14 h-[5px] w-3.5 rounded-[2px]"></span>
              <span class="bg-foreground/14 h-[5px] w-3.5 rounded-[2px]"></span>
            </span>
          </span>
          <span class="flex-1"></span>
          <span
            class="border-foreground/8 bg-card h-3 w-[62%] rounded-full border"></span>
        }

        @default {
          <span class="flex flex-1 flex-col gap-1.5 p-2">
            <span class="bg-foreground/50 h-2 w-[70%] rounded-[3px]"></span>
            <span class="flex gap-1">
              <span class="bg-primary h-[7px] w-[26px] rounded-[3px]"></span>
              <span
                class="bg-foreground/18 h-[7px] w-[22px] rounded-[3px]"></span>
              <span
                class="bg-foreground/18 h-[7px] w-[18px] rounded-[3px]"></span>
            </span>
            <span class="bg-foreground/12 h-1 rounded-[2px]"></span>
            <span class="bg-foreground/12 h-1 w-[85%] rounded-[2px]"></span>
            <span class="bg-foreground/12 h-1 w-3/5 rounded-[2px]"></span>
            <span class="flex-1"></span>
            <span class="border-foreground/8 flex gap-1 border-t pt-[5px]">
              <span class="bg-foreground/30 h-[5px] w-5 rounded-[2px]"></span>
              <span class="bg-foreground/14 h-[5px] w-4 rounded-[2px]"></span>
              <span class="bg-foreground/14 h-[5px] w-4 rounded-[2px]"></span>
            </span>
          </span>

          <span
            class="border-foreground/8 bg-hover flex w-[74px] flex-col gap-1.5 border-l px-1.5 py-2">
            <span class="bg-foreground/30 h-1.5 w-[34px] rounded-[2px]"></span>
            @for (row of fieldRows; track $index) {
              <span class="flex items-center gap-1">
                <span class="bg-foreground/20 h-2.5 w-2.5 rounded-full"></span>
                <span class="bg-foreground/14 h-1 flex-1 rounded-[2px]"></span>
              </span>
            }
            <span class="flex-1"></span>
            <span class="border-foreground/8 h-3 rounded-md border"></span>
          </span>
        }
      }
    </span>
  `,
})
export class TaskDetailLayoutPreviewComponent {
  readonly layout = input.required<string>();

  protected readonly fieldRows = Array.from({ length: 2 });
  protected readonly commentRows = Array.from({ length: 2 });
}
