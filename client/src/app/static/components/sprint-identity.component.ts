import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { booleanAttribute, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { LucideCalendarClock } from '@lucide/angular';
import { cn } from './button/button.variants';
import { IconTileComponent } from './icon-tile.component';
import { SprintDaysBadgeComponent } from './sprint-days-badge.component';
import { SprintStatusBadgeComponent } from './sprint-status-badge.component';

export type SprintIdentitySize = 'small' | 'medium' | 'large';
export type SprintIdentityVariant = 'plain' | 'card';

const containerClasses: Record<SprintIdentityVariant, string> = {
  plain: 'flex items-start gap-3',
  card: 'border-border bg-card flex items-start gap-3 rounded-lg border',
};

const cardPaddingClasses: Record<SprintIdentitySize, string> = {
  small: 'px-4 py-3',
  medium: 'px-6 py-5',
  large: 'px-6 py-5',
};

const nameClasses: Record<SprintIdentitySize, string> = {
  small: 'text-base',
  medium: 'text-lg',
  large: 'text-xl',
};

const metaClasses: Record<SprintIdentitySize, string> = {
  small: 'mt-0.5 text-[13px]',
  medium: 'mt-1 text-sm',
  large: 'mt-1 text-sm',
};

const goalClasses: Record<SprintIdentitySize, string> = {
  small: 'mt-1.5 line-clamp-2 text-[13px] leading-[1.45]',
  medium: 'mt-2 line-clamp-2 text-sm',
  large: 'mt-2 text-sm',
};

@Component({
  selector: 'app-sprint-identity',
  host: { class: 'contents' },
  imports: [
    DatePipe,
    NgTemplateOutlet,
    RouterLink,
    IconTileComponent,
    SprintDaysBadgeComponent,
    SprintStatusBadgeComponent,
  ],
  template: `
    <div [class]="containerClass()">
      <app-icon-tile [icon]="sprintIcon" />

      <div class="min-w-0 flex-1">
        @if (eyebrow(); as eyebrow) {
          <p class="text-muted text-xs font-semibold tracking-wide uppercase">
            {{ eyebrow }}
          </p>
        }

        <div class="flex flex-wrap items-center gap-2" [class.mt-1]="eyebrow()">
          @switch (headingLevel()) {
            @case (1) {
              <h1 class="contents">
                <ng-container [ngTemplateOutlet]="name" />
              </h1>
            }
            @case (2) {
              <h2 class="contents">
                <ng-container [ngTemplateOutlet]="name" />
              </h2>
            }
            @default {
              <ng-container [ngTemplateOutlet]="name" />
            }
          }

          <app-sprint-status-badge [status]="sprint().status" />
          <app-sprint-days-badge
            [status]="sprint().status"
            [endDate]="sprint().endDate" />
        </div>

        <p [class]="metaClass()">
          <span class="font-medium">{{ sprint().projectName }}</span>
          &nbsp;·&nbsp;
          {{ sprint().startDate | date: 'mediumDate' }} –
          {{ sprint().endDate | date: 'mediumDate' }}
        </p>

        @if (showGoal() && sprint().goal; as goal) {
          <p [class]="goalClass()">{{ goal }}</p>
        }
      </div>
    </div>

    <ng-template #name>
      @if (link(); as route) {
        <a [class]="nameClass() + ' hover:underline'" [routerLink]="route">
          {{ sprint().name }}
        </a>
      } @else {
        <span [class]="nameClass()">{{ sprint().name }}</span>
      }
    </ng-template>
  `,
})
export class SprintIdentityComponent {
  readonly sprint = input.required<SprintViewModel>();
  readonly size = input<SprintIdentitySize>('medium');
  readonly variant = input<SprintIdentityVariant>('plain');
  readonly eyebrow = input('');
  readonly link = input<unknown[] | null>(null);
  readonly headingLevel = input<0 | 1 | 2>(0);
  readonly showGoal = input(false, { transform: booleanAttribute });
  readonly class = input('');

  protected readonly sprintIcon = LucideCalendarClock;

  protected readonly containerClass = computed(() => {
    const isCard = this.variant() === 'card';

    return cn(
      containerClasses[this.variant()],
      isCard ? cardPaddingClasses[this.size()] : '',
      this.class()
    );
  });

  protected readonly nameClass = computed(() =>
    cn(
      'font-overpass text-foreground truncate font-semibold',
      nameClasses[this.size()]
    )
  );

  protected readonly metaClass = computed(() =>
    cn('text-muted', metaClasses[this.size()])
  );

  protected readonly goalClass = computed(() =>
    cn('text-muted', goalClasses[this.size()])
  );
}
