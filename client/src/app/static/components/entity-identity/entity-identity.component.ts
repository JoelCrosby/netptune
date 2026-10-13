import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { type LucideIconInput } from '@lucide/angular';
import { cn } from '../button/button.variants';
import { IconTileComponent } from '../icon-tile.component';

export type EntityIdentitySize = 'small' | 'medium' | 'large';
export type EntityIdentityVariant = 'plain' | 'card';

const containerClasses: Record<EntityIdentityVariant, string> = {
  plain: 'flex items-start gap-3',
  card: 'border-border bg-card flex items-start gap-3 rounded-lg border',
};

const cardPaddingClasses: Record<EntityIdentitySize, string> = {
  small: 'px-4 py-3',
  medium: 'px-6 py-5',
  large: 'px-6 py-5',
};

const nameClasses: Record<EntityIdentitySize, string> = {
  small: 'text-base',
  medium: 'text-lg',
  large: 'text-xl',
};

const metaClasses: Record<EntityIdentitySize, string> = {
  small: 'mt-0.5 text-[13px]',
  medium: 'mt-1 text-sm',
  large: 'mt-1 text-sm',
};

const descriptionClasses: Record<EntityIdentitySize, string> = {
  small: 'mt-1.5 line-clamp-2 text-[13px] leading-[1.45]',
  medium: 'mt-2 line-clamp-2 text-sm',
  large: 'mt-2 text-sm',
};

@Component({
  selector: 'app-entity-identity',
  host: { class: 'contents' },
  imports: [IconTileComponent, NgTemplateOutlet, RouterLink],
  template: `
    <div [class]="containerClass()">
      <app-icon-tile [icon]="icon()" />

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
                <ng-container [ngTemplateOutlet]="nameTemplate" />
              </h1>
            }
            @case (2) {
              <h2 class="contents">
                <ng-container [ngTemplateOutlet]="nameTemplate" />
              </h2>
            }
            @default {
              <ng-container [ngTemplateOutlet]="nameTemplate" />
            }
          }

          <ng-content select="[identityBadges]" />
        </div>

        @if (metaLead() || meta()) {
          <p [class]="metaClass()">
            @if (metaLead(); as lead) {
              <span class="font-medium">{{ lead }}</span>
              @if (meta()) {
                &nbsp;·&nbsp;
              }
            }
            {{ meta() }}
          </p>
        }

        @if (description(); as description) {
          <p [class]="descriptionClass()">{{ description }}</p>
        }
      </div>
    </div>

    <ng-template #nameTemplate>
      @if (link(); as route) {
        <a [class]="nameClass() + ' hover:underline'" [routerLink]="route">
          {{ name() }}
        </a>
      } @else {
        <span [class]="nameClass()">{{ name() }}</span>
      }
    </ng-template>
  `,
})
export class EntityIdentityComponent {
  readonly icon = input.required<LucideIconInput>();
  readonly name = input.required<string>();
  readonly eyebrow = input('');
  readonly metaLead = input('');
  readonly meta = input('');
  readonly description = input('');
  readonly link = input<unknown[] | null>(null);
  readonly headingLevel = input<0 | 1 | 2>(0);
  readonly size = input<EntityIdentitySize>('medium');
  readonly variant = input<EntityIdentityVariant>('plain');
  readonly class = input('');

  protected readonly containerClass = computed(() => {
    const isCard = this.variant() === 'card';

    return cn(
      containerClasses[this.variant()],
      isCard ? cardPaddingClasses[this.size()] : '',
      this.class()
    );
  });

  protected readonly nameClass = computed(() => {
    return cn(
      'font-overpass text-foreground truncate font-semibold',
      nameClasses[this.size()]
    );
  });

  protected readonly metaClass = computed(() => {
    return cn('text-muted', metaClasses[this.size()]);
  });

  protected readonly descriptionClass = computed(() => {
    return cn('text-muted', descriptionClasses[this.size()]);
  });
}
