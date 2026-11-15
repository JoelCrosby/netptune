import { Component, input } from '@angular/core';

export interface DetailListEntry {
  text: string;
  detail?: string | null;
  note?: string | null;
}

@Component({
  selector: 'app-detail-list',
  host: { class: 'block' },
  template: `
    <ul class="flex min-w-0 flex-col gap-1.5">
      @for (entry of entries(); track $index) {
        <li class="flex min-w-0 flex-col gap-0.5">
          <span class="flex min-w-0 items-center gap-2">
            <span class="font-avatar text-muted text-[11.5px] break-all">
              {{ entry.text }}
            </span>
            @if (entry.note; as note) {
              <span class="text-warn font-avatar shrink-0 text-[11.5px]">
                {{ note }}
              </span>
            }
          </span>
          @if (entry.detail; as detail) {
            <span class="text-muted text-xs">{{ detail }}</span>
          }
        </li>
      }
    </ul>
  `,
})
export class DetailListComponent {
  readonly entries = input<readonly DetailListEntry[]>([]);
}
