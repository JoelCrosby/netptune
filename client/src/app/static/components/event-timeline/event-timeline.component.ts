import { Component, computed, input, output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  LucideChevronDown,
  LucideDynamicIcon,
  type LucideIconInput,
} from '@lucide/angular';
import { TooltipDirective } from '../../directives/tooltip.directive';
import { AvatarComponent } from '../avatar/avatar.component';
import { type Tone } from '../tones';

export type EventTimelineSurface = 'dialog' | 'card' | 'background';

export interface EventTimelineActor {
  name: string;
  pictureUrl: string | null;
  isServiceAccount: boolean;
}

export interface EventTimelineSegment {
  text: string;
  strong?: boolean;
}

export interface EventTimelineRow {
  key: string;
  actor: EventTimelineActor;
  actorNote?: string | null;
  icon: LucideIconInput;
  tone: Tone;
  summary: readonly EventTimelineSegment[];
  note?: string | null;
  subjectId?: string | null;
  subjectLink?: readonly unknown[] | null;
  subjectLabel: string;
  subjectStruck?: boolean;
  time: string;
  timeTitle: string;
  repeatLabel?: string | null;
  repeatTimes?: readonly string[];
}

export interface EventTimelineGroup {
  key: string;
  label: string;
  rows: EventTimelineRow[];
}

const badgeToneClasses: Record<Tone, string> = {
  neutral: 'text-foreground/60',
  purple: 'text-primary',
  green: 'text-emerald-600 dark:text-emerald-300',
  blue: 'text-blue-600 dark:text-blue-300',
  amber: 'text-amber-600 dark:text-amber-300',
  teal: 'text-teal-600 dark:text-teal-300',
  red: 'text-red-600 dark:text-red-300',
  pink: 'text-fuchsia-600 dark:text-fuchsia-300',
};

const surfaceClasses: Record<EventTimelineSurface, string> = {
  dialog: 'bg-dialog-background',
  card: 'bg-card',
  background: 'bg-background',
};

const surfaceRingClasses: Record<EventTimelineSurface, string> = {
  dialog: 'ring-dialog-background',
  card: 'ring-card',
  background: 'ring-background',
};

@Component({
  selector: 'app-event-timeline',
  imports: [
    AvatarComponent,
    LucideChevronDown,
    LucideDynamicIcon,
    RouterLink,
    TooltipDirective,
  ],
  host: { class: 'block' },
  template: `
    @for (group of groups(); track group.key) {
      <section>
        <h3
          class="text-foreground/50 sticky top-0 z-[2] flex items-center gap-2.5 px-5 pt-2.5 pb-1.5 text-[11.5px] font-bold tracking-[0.07em] uppercase"
          [class]="surfaceClass()">
          <span>{{ group.label }}</span>
          <span class="bg-foreground/9 h-px flex-1" aria-hidden="true"></span>
        </h3>

        <ol>
          @for (row of group.rows; track row.key) {
            @let open = expanded().has(row.key);
            <li
              class="hover:bg-foreground/3 relative flex gap-3.5 px-5 py-[9px] transition-colors">
              <span
                class="bg-border absolute inset-y-0 left-9 w-px"
                aria-hidden="true"></span>

              <span
                class="relative h-8 w-8 shrink-0 rounded-full ring-3"
                [class]="ringClass()">
                <app-avatar
                  size="md"
                  [tooltip]="false"
                  [name]="row.actor.name"
                  [imageUrl]="row.actor.pictureUrl"
                  [isServiceAccount]="row.actor.isServiceAccount" />
                <span
                  class="absolute -right-1 -bottom-1 flex h-[17px] w-[17px] items-center justify-center rounded-full"
                  [class]="[surfaceClass(), badgeToneClasses[row.tone]]"
                  aria-hidden="true">
                  <svg [lucideIcon]="row.icon" class="h-[11px] w-[11px]"></svg>
                </span>
              </span>

              <div class="flex min-w-0 flex-1 flex-col gap-[3px] pt-1">
                <p class="text-[13.5px]/[1.5] text-pretty">
                  <span class="font-semibold">{{ row.actor.name }}</span>
                  @if (row.actorNote) {
                    <span class="text-muted text-xs"> {{ row.actorNote }}</span>
                  }
                  @for (segment of row.summary; track $index) {
                    <span
                      [class]="
                        segment.strong ? 'font-semibold' : 'text-foreground/60'
                      "
                      >{{ $first ? ' ' : '' }}{{ segment.text }}</span
                    >
                  }
                  @if (row.note) {
                    <span class="text-foreground/60"> {{ row.note }}</span>
                  }
                  @if (row.repeatLabel) {
                    <button
                      type="button"
                      class="text-primary focus-visible:ring-primary ml-1.5 inline-flex cursor-pointer items-center gap-[3px] rounded-xs align-baseline text-[12.5px] font-bold focus-visible:ring-2 focus-visible:outline-none"
                      [attr.aria-expanded]="open"
                      (click)="toggle(row.key)">
                      {{ row.repeatLabel }}
                      <svg
                        lucideChevronDown
                        class="h-3 w-3 transition-transform duration-150"
                        [class.rotate-180]="open"
                        aria-hidden="true"></svg>
                    </button>
                  }
                </p>

                <div class="flex min-w-0 items-center gap-2 text-[12.5px]">
                  @if (row.subjectId) {
                    @if (row.subjectLink) {
                      <a
                        class="font-avatar text-foreground/70 hover:text-foreground shrink-0 text-[11.5px] underline-offset-2 hover:underline"
                        [routerLink]="row.subjectLink"
                        (click)="subjectClicked.emit(row)">
                        {{ row.subjectId }}
                      </a>
                    } @else {
                      <span
                        class="font-avatar text-foreground/70 shrink-0 text-[11.5px]">
                        {{ row.subjectId }}
                      </span>
                    }
                  }
                  <span
                    class="text-foreground/50 min-w-0 truncate"
                    [class.line-through]="row.subjectStruck">
                    {{ row.subjectLabel }}
                  </span>
                </div>

                @if (open && row.repeatTimes?.length) {
                  <ol class="mt-1 flex flex-col gap-0.5">
                    @for (time of row.repeatTimes; track $index) {
                      <li class="text-foreground/50 flex gap-2.5 text-xs">
                        <span class="font-avatar min-w-11">{{ time }}</span>
                        <span>
                          @for (segment of row.summary; track $index) {
                            <span>{{ segment.text }}</span>
                          }
                        </span>
                      </li>
                    }
                  </ol>
                }
              </div>

              <span
                class="text-foreground/45 shrink-0 pt-[5px] text-xs whitespace-nowrap"
                [appTooltip]="row.timeTitle">
                {{ row.time }}
              </span>
            </li>
          }
        </ol>
      </section>
    }
  `,
})
export class EventTimelineComponent {
  readonly groups = input.required<EventTimelineGroup[]>();
  readonly surface = input<EventTimelineSurface>('dialog');

  readonly subjectClicked = output<EventTimelineRow>();

  protected readonly expanded = signal<ReadonlySet<string>>(new Set());
  protected readonly badgeToneClasses = badgeToneClasses;

  protected readonly surfaceClass = computed(() => {
    return surfaceClasses[this.surface()];
  });

  protected readonly ringClass = computed(() => {
    return surfaceRingClasses[this.surface()];
  });

  protected toggle(key: string) {
    this.expanded.update((keys) => {
      const next = new Set(keys);

      if (next.has(key)) {
        next.delete(key);
      } else {
        next.add(key);
      }

      return next;
    });
  }
}
