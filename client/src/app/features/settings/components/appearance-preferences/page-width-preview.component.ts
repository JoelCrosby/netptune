import { Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-page-width-preview',
  host: { class: 'block' },
  template: `
    <span
      class="border-foreground/8 bg-foreground/2 flex h-[116px] overflow-hidden rounded-md border"
      aria-hidden="true">
      <span class="bg-foreground/12 w-[22px] shrink-0"></span>

      <span class="flex min-w-0 flex-1 flex-col">
        <span class="border-foreground/8 border-b px-1.5 py-[7px]">
          <span class="mx-auto flex items-center gap-1" [class]="columnClass()">
            <span class="bg-foreground/50 h-2 w-[36px] rounded-[3px]"></span>
            <span class="flex-1"></span>
            <span class="bg-primary h-2 w-[24px] rounded-[3px]"></span>
          </span>
        </span>

        <span class="flex flex-1 flex-col px-1.5 py-2">
          <span class="mx-auto flex flex-col gap-[7px]" [class]="columnClass()">
            <span class="border-foreground/8 flex gap-1 border-b pb-1.5">
              <span class="bg-foreground/30 h-1 flex-1 rounded-[2px]"></span>
              <span class="bg-foreground/30 h-1 w-[18%] rounded-[2px]"></span>
              <span class="bg-foreground/30 h-1 w-[14%] rounded-[2px]"></span>
            </span>

            @for (row of rows; track $index) {
              <span class="flex items-center gap-1">
                <span
                  class="bg-foreground/16 h-1.5 flex-1 rounded-[2px]"></span>
                <span
                  class="bg-foreground/12 h-1.5 w-[18%] rounded-[2px]"></span>
                <span
                  class="bg-foreground/22 h-1.5 w-[14%] rounded-[3px]"></span>
              </span>
            }
          </span>
        </span>
      </span>
    </span>
  `,
})
export class PageWidthPreviewComponent {
  readonly width = input.required<string>();

  protected readonly rows = Array.from({ length: 4 });

  protected readonly columnClass = computed(() => {
    return this.width() === 'full' ? 'w-full' : 'w-[64%]';
  });
}
