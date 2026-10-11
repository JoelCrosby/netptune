import { Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucidePlus } from '@lucide/angular';
import { AvatarComponent } from '../avatar/avatar.component';
import { BadgeComponent } from '../badge/badge.component';
import { EmptyStateComponent } from '../empty-state/empty-state.component';
import { IconTileComponent } from '../icon-tile.component';
import { DocumentListItem } from './document-list-item';

@Component({
  selector: 'app-document-card-grid',
  imports: [
    RouterLink,
    AvatarComponent,
    BadgeComponent,
    EmptyStateComponent,
    IconTileComponent,
  ],
  host: { class: 'block @container' },
  template: `
    @if (items().length || addLabel()) {
      <ul
        class="grid grid-cols-1 gap-4 @xl:grid-cols-2 @3xl:grid-cols-3"
        [attr.aria-label]="ariaLabel()">
        @for (item of items(); track item.id) {
          <li class="flex">
            <a
              class="border-border bg-card hover:border-primary focus-visible:ring-primary flex w-full flex-col gap-3 rounded-xl border p-[18px] transition-colors focus-visible:ring-2 focus-visible:outline-none"
              [routerLink]="item.link">
              <span class="flex items-start justify-between gap-2">
                <app-icon-tile
                  size="medium"
                  [icon]="item.icon"
                  [tone]="item.tone" />
                <app-badge [color]="item.statusTone">
                  {{ item.statusLabel }}
                </app-badge>
              </span>

              <span class="flex flex-1 flex-col gap-1">
                <span
                  class="text-foreground text-[15.5px] leading-[1.3] font-semibold">
                  {{ item.title }}
                </span>
                @if (item.excerpt) {
                  <span
                    class="text-foreground/60 line-clamp-2 text-[13.5px] leading-normal">
                    {{ item.excerpt }}
                  </span>
                }
              </span>

              <span
                class="border-border text-foreground/60 flex min-w-0 items-center gap-1.5 border-t pt-3 text-[12.5px]">
                <app-avatar
                  size="sm"
                  class="shrink-0"
                  [name]="item.editorName"
                  [imageUrl]="item.editorPictureUrl" />
                <span class="truncate">{{ item.editedLabel }}</span>
                @if (item.readLabel) {
                  <span aria-hidden="true">·</span>
                  <span class="shrink-0">{{ item.readLabel }}</span>
                }
              </span>
            </a>
          </li>
        }

        @if (addLabel(); as label) {
          <li class="flex">
            <button
              type="button"
              class="border-border text-foreground/60 hover:border-primary hover:text-primary focus-visible:ring-primary flex min-h-[180px] w-full cursor-pointer flex-col items-center justify-center gap-3 rounded-xl border border-dashed p-[18px] text-center text-[13.5px] transition-colors focus-visible:ring-2 focus-visible:outline-none"
              (click)="added.emit()">
              <app-icon-tile size="medium" [icon]="addIcon" />
              <span>{{ label }}</span>
            </button>
          </li>
        }
      </ul>
    } @else {
      <app-empty-state
        outlined
        compact
        [title]="emptyHeading()"
        [description]="emptyDescription()" />
    }
  `,
})
export class DocumentCardGridComponent {
  readonly items = input.required<readonly DocumentListItem[]>();
  readonly ariaLabel = input.required<string>();
  readonly emptyHeading = input.required<string>();
  readonly emptyDescription = input('');
  readonly addLabel = input<string | null>(null);

  readonly added = output();

  protected readonly addIcon = LucidePlus;
}
