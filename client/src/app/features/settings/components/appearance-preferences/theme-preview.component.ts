import { Component, computed, input } from '@angular/core';

interface ThemePreviewPalette {
  frame: string;
  sidebar: string;
  sidebarBarStrong: string;
  sidebarBarFaint: string;
  appBar: string;
  card: string;
  bar: string;
}

const palettes: Record<string, ThemePreviewPalette> = {
  light: {
    frame: 'border-[rgb(214,214,219)] bg-[#fafafb]',
    sidebar: 'bg-[rgb(41,36,46)]',
    sidebarBarStrong: 'bg-white/50',
    sidebarBarFaint: 'bg-white/22',
    appBar: 'border-[rgb(226,226,231)] bg-white',
    card: 'border-[rgb(226,226,231)] bg-white',
    bar: 'bg-black/10',
  },
  dark: {
    frame: 'border-[rgb(41,41,41)] bg-black',
    sidebar: 'bg-[#080808]',
    sidebarBarStrong: 'bg-white/45',
    sidebarBarFaint: 'bg-white/18',
    appBar: 'border-[rgb(41,41,41)] bg-[#272727]',
    card: 'border-[rgb(41,41,41)] bg-[rgb(10,10,10)]',
    bar: 'bg-white/14',
  },
};

@Component({
  selector: 'app-theme-preview',
  host: { class: 'block' },
  template: `
    <span
      class="flex h-[116px] overflow-hidden rounded-md border"
      [class]="palette().frame"
      aria-hidden="true">
      <span
        class="flex w-[34px] flex-col gap-1.25 px-1.5 py-2"
        [class]="palette().sidebar">
        <span
          class="h-1.25 rounded-[2px]"
          [class]="palette().sidebarBarStrong"></span>
        @for (bar of faintBars; track $index) {
          <span
            class="h-1.25 rounded-[2px]"
            [class]="palette().sidebarBarFaint"></span>
        }
      </span>

      <span class="flex flex-1 flex-col">
        <span class="h-4 border-b" [class]="palette().appBar"></span>
        <span class="flex flex-1 flex-col gap-1.5 p-2">
          <span
            class="h-[26px] rounded-[4px] border"
            [class]="palette().card"></span>
          <span
            class="h-[26px] rounded-[4px] border"
            [class]="palette().card"></span>
          <span class="h-2 w-3/5 rounded-[3px]" [class]="palette().bar"></span>
        </span>
      </span>
    </span>
  `,
})
export class ThemePreviewComponent {
  readonly theme = input.required<string>();

  protected readonly faintBars = Array.from({ length: 3 });

  protected readonly palette = computed(
    () => palettes[this.theme()] ?? palettes['light']
  );
}
