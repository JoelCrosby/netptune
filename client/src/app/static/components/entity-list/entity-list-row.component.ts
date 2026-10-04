import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { colorBackgroundClass } from '@core/util/colors/colors';
import { LucideChevronRight } from '@lucide/angular';
import { AvatarStackComponent } from '../avatar-stack/avatar-stack.component';
import { IconTileComponent } from '../icon-tile.component';
import { FromNowPipe } from '@static/pipes/from-now.pipe';
import { entityListColumnsClass } from './entity-list.columns';
import { EntityListItem } from './entity-list.types';

const MAX_VISIBLE_PEOPLE = 4;

// One item in an `app-entity-list`, linking through to the item.
@Component({
  selector: 'app-entity-list-row',
  imports: [
    RouterLink,
    AvatarStackComponent,
    IconTileComponent,
    FromNowPipe,
    LucideChevronRight,
  ],
  host: {
    class: 'border-border block border-b last:border-b-0',
  },
  template: `
    <a [class]="linkClass" [routerLink]="item().link">
      <span class="flex min-w-0 items-center gap-3">
        @if (item().imageUrl; as url) {
          <img
            [src]="url"
            alt=""
            class="border-border h-8 w-8 shrink-0 rounded-lg border object-cover" />
        } @else {
          <app-icon-tile [icon]="item().icon" [class]="tileClass()" />
        }

        <span class="flex min-w-0 flex-col gap-0.5">
          <span class="truncate text-sm font-semibold">{{ item().name }}</span>
          @if (item().identifier; as identifier) {
            <span class="text-muted truncate font-mono text-[11px]">
              {{ identifier }}
            </span>
          }
        </span>
      </span>

      <span class="flex items-center gap-1.5 max-md:hidden">
        @if (visiblePeople().length) {
          <app-avatar-stack [avatars]="visiblePeople()" />

          @if (hiddenPeopleCount(); as hidden) {
            <span class="text-muted text-xs font-medium">+{{ hidden }}</span>
          }
        }
      </span>

      <span class="text-right text-sm font-semibold tabular-nums">
        {{ item().count }}
      </span>

      <span class="text-muted truncate text-sm max-md:hidden">
        {{ item().updatedAt | fromNow }}
      </span>

      <svg
        lucideChevronRight
        class="text-foreground/25 h-4 w-4"
        aria-hidden="true"></svg>
    </a>
  `,
})
export class EntityListRowComponent {
  readonly item = input.required<EntityListItem>();

  protected readonly linkClass = `${entityListColumnsClass} bg-card hover:bg-card-hover focus-visible:bg-card-hover min-h-15 py-2.5 transition-colors outline-none`;

  protected readonly tileClass = computed(() => {
    return `${colorBackgroundClass(this.item().color)} h-8 w-8 text-white`;
  });

  protected readonly visiblePeople = computed(() => {
    return this.item().people.slice(0, MAX_VISIBLE_PEOPLE);
  });

  protected readonly hiddenPeopleCount = computed(() => {
    return Math.max(this.item().people.length - MAX_VISIBLE_PEOPLE, 0);
  });
}
