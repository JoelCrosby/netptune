import { Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucidePlus } from '@lucide/angular';
import { AvatarComponent } from '../avatar/avatar.component';
import { BadgeComponent } from '../badge/badge.component';
import { InlineButtonComponent } from '../button/inline-button.component';
import { EmptyStateComponent } from '../empty-state/empty-state.component';
import { IconTileComponent } from '../icon-tile.component';
import { DocumentListItem } from './document-list-item';

const columns =
  'grid grid-cols-[minmax(0,1fr)_90px_160px_130px] items-center gap-6 @max-3xl:grid-cols-[minmax(0,1fr)_90px_130px] @max-xl:grid-cols-[minmax(0,1fr)_auto]';

@Component({
  selector: 'app-document-table',
  imports: [
    RouterLink,
    AvatarComponent,
    BadgeComponent,
    EmptyStateComponent,
    IconTileComponent,
    InlineButtonComponent,
    LucidePlus,
  ],
  host: { class: 'block @container' },
  template: `
    @if (items().length) {
      <div class="border-border bg-card overflow-hidden rounded-xl border">
        <div
          aria-hidden="true"
          [class]="columns"
          class="border-border text-foreground/50 border-b px-5 py-2.5 text-[11.5px] font-semibold tracking-[0.06em] uppercase">
          <span>{{ titleHeader() }}</span>
          <span class="@max-xl:hidden">{{ statusHeader() }}</span>
          <span class="@max-3xl:hidden">{{ tagsHeader() }}</span>
          <span class="@max-xl:hidden">{{ editedHeader() }}</span>
        </div>

        <ul [attr.aria-label]="ariaLabel()">
          @for (item of items(); track item.id) {
            <li class="border-border border-b last:border-b-0">
              <a
                [class]="columns"
                class="hover:bg-foreground/4 focus-visible:ring-primary px-5 py-3.5 transition-colors focus-visible:ring-2 focus-visible:outline-none focus-visible:ring-inset"
                [routerLink]="item.link">
                <span class="flex min-w-0 items-center gap-3">
                  <app-icon-tile
                    size="medium"
                    [icon]="item.icon"
                    [tone]="item.tone" />
                  <span class="flex min-w-0 flex-col gap-0.5">
                    <span
                      class="text-foreground truncate text-[14.5px] font-semibold">
                      {{ item.title }}
                    </span>
                    @if (item.excerpt) {
                      <span class="text-foreground/60 truncate text-[13px]">
                        {{ item.excerpt }}
                      </span>
                    }
                  </span>
                </span>

                <span class="flex @max-xl:hidden">
                  <app-badge [color]="item.statusTone">
                    {{ item.statusLabel }}
                  </app-badge>
                </span>

                <span class="flex flex-wrap gap-1 @max-3xl:hidden">
                  @for (tag of item.tags; track tag) {
                    <app-badge shape="rounded">{{ tag }}</app-badge>
                  }
                </span>

                <span class="flex min-w-0 items-center gap-2">
                  <app-avatar
                    size="sm"
                    [name]="item.editorName"
                    [imageUrl]="item.editorPictureUrl" />
                  <span
                    class="text-foreground/60 truncate text-[13px] @max-xl:hidden">
                    {{ item.editedLabel }}
                  </span>
                </span>
              </a>
            </li>
          }
        </ul>

        @if (addLabel(); as label) {
          <button
            type="button"
            class="border-border text-foreground/60 hover:bg-foreground/4 hover:text-primary focus-visible:ring-primary flex w-full cursor-pointer items-center gap-2 border-t px-5 py-3.5 text-left text-[13.5px] transition-colors focus-visible:ring-2 focus-visible:outline-none focus-visible:ring-inset"
            (click)="added.emit()">
            <svg lucidePlus class="h-4 w-4 shrink-0"></svg>
            <span>{{ label }}</span>
          </button>
        }
      </div>
    } @else {
      <app-empty-state
        outlined
        compact
        [title]="emptyHeading()"
        [description]="emptyDescription()">
        @if (addLabel(); as label) {
          <button
            emptyStateAction
            app-inline-button
            type="button"
            (click)="added.emit()">
            <svg lucidePlus class="h-4 w-4 shrink-0"></svg>
            <span>{{ label }}</span>
          </button>
        }
      </app-empty-state>
    }
  `,
})
export class DocumentTableComponent {
  readonly items = input.required<readonly DocumentListItem[]>();
  readonly ariaLabel = input.required<string>();
  readonly titleHeader = input.required<string>();
  readonly statusHeader = input.required<string>();
  readonly tagsHeader = input.required<string>();
  readonly editedHeader = input.required<string>();
  readonly emptyHeading = input.required<string>();
  readonly emptyDescription = input('');
  readonly addLabel = input<string | null>(null);

  readonly added = output();

  protected readonly columns = columns;
}
