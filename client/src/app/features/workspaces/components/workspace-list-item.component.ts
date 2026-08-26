import {
  Component,
  ElementRef,
  computed,
  effect,
  inject,
  input,
  output,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { Workspace } from '@core/models/workspace';
import { ThemeService } from '@core/services/theme.service';
import { brandingImageUrl } from '@core/util/branding';
import { avatarColors, colorHex } from '@core/util/colors/colors';
import { workspaceBrandVariables } from '@core/util/colors/workspace-branding';
import { LucideChevronRight, LucidePin } from '@lucide/angular';
import { FromNowPipe } from '@static/pipes/from-now.pipe';

interface RowAvatar {
  id: string;
  initials: string;
  color: string;
}

const maxAvatars = 5;

@Component({
  selector: 'app-workspace-list-item',
  imports: [FromNowPipe, LucideChevronRight, LucidePin, RouterLink],
  host: { class: 'block' },
  styles: `
    .workspace-color-wash {
      background: radial-gradient(
        ellipse 55% 130% at 0% 50%,
        rgba(var(--primary-rgb), 0.12) 0%,
        transparent 70%
      );
    }

    .workspace-row {
      border-color: var(--border);
    }

    .workspace-row.last-visited {
      background-image: linear-gradient(
        rgba(var(--foreground-rgb), 0.015),
        rgba(var(--foreground-rgb), 0.015)
      );
      border-color: rgba(var(--foreground-rgb), 0.16);
    }

    .workspace-row:hover,
    .workspace-row:focus-within {
      border-color: rgba(var(--primary-rgb), 0.5);
    }

    .workspace-row:hover {
      transform: translateY(-1px);
    }

    .pin-button {
      color: rgba(var(--foreground-rgb), 0.28);
    }

    .pin-button:hover {
      background: rgba(var(--foreground-rgb), 0.08);
      color: rgba(var(--foreground-rgb), 0.85);
    }

    .pin-button.pinned {
      color: var(--primary);
    }

    @media (prefers-reduced-motion: reduce) {
      .workspace-row {
        transition: none;
      }

      .workspace-row:hover {
        transform: none;
      }
    }
  `,
  template: `
    <div
      class="workspace-row bg-card relative flex cursor-pointer items-center gap-4 overflow-hidden rounded-[14px] border p-4 transition-[border-color,background,transform] duration-180 sm:gap-4.5 sm:p-5"
      [class.last-visited]="workspace().isLastVisited"
      (click)="open.emit()">
      <span
        aria-hidden="true"
        class="workspace-color-wash pointer-events-none absolute inset-0"></span>

      <span
        class="font-overpass relative flex h-11 w-11 shrink-0 items-center justify-center overflow-hidden rounded-[11px] text-lg font-semibold text-white sm:h-13 sm:w-13 sm:rounded-[13px] sm:text-[22px]"
        [style.background]="logoUrl() ? null : badgeColor()"
        [style.box-shadow]="logoUrl() ? null : badgeGlow()">
        @if (logoUrl(); as url) {
          <img [src]="url" alt="" class="h-full w-full object-cover" />
        } @else {
          {{ letter() }}
        }
      </span>

      <span class="relative flex min-w-0 flex-1 flex-col gap-1.25">
        <span class="flex min-w-0 items-center gap-2.25">
          <a
            class="font-overpass focus-visible:outline-primary min-w-0 overflow-hidden rounded-sm text-[17px] font-medium text-ellipsis whitespace-nowrap text-[rgb(var(--foreground-rgb))] no-underline focus-visible:outline-2 focus-visible:outline-offset-[3px]"
            [routerLink]="['/', workspace().slug, 'projects']"
            (click)="onNameClicked($event)">
            {{ workspace().name }}
          </a>
          @if (workspace().isLastVisited) {
            <span
              class="shrink-0 rounded-full bg-[rgba(var(--foreground-rgb),0.09)] px-1.75 py-0.5 text-[10.5px] tracking-[0.04em] whitespace-nowrap text-[rgba(var(--foreground-rgb),0.6)] uppercase"
              i18n="Label marking the workspace the user last opened">
              Last visited
            </span>
          }
        </span>

        @if (workspace().description; as description) {
          <span
            class="hidden overflow-hidden text-[13px] text-ellipsis whitespace-nowrap text-[rgba(var(--foreground-rgb),0.55)] sm:block">
            {{ description }}
          </span>
        }

        <span
          class="mt-1.25 flex min-w-0 flex-wrap items-center gap-x-2.5 gap-y-1">
          @if (avatars().length) {
            <span class="flex shrink-0 items-center" aria-hidden="true">
              @for (avatar of avatars(); track avatar.id; let first = $first) {
                <span
                  class="border-card flex h-5.75 w-5.75 items-center justify-center rounded-full border-2 text-[10.5px] font-medium text-white"
                  [style.margin-left]="first ? null : '-7px'"
                  [style.background-color]="avatar.color">
                  {{ avatar.initials }}
                </span>
              }
            </span>
          }

          @if (memberCount(); as count) {
            <span
              class="shrink-0 text-[12.5px] whitespace-nowrap text-[rgba(var(--foreground-rgb),0.52)]">
              {{ count }}
              <ng-container i18n="Number of people in a workspace">
                {count, plural, =1 {member} other {members}}
              </ng-container>
            </span>
            <span
              aria-hidden="true"
              class="h-0.75 w-0.75 shrink-0 rounded-full bg-[rgba(var(--foreground-rgb),0.22)]"></span>
          }

          <span
            class="min-w-0 overflow-hidden text-[12.5px] text-ellipsis whitespace-nowrap text-[rgba(var(--foreground-rgb),0.52)]">
            <ng-container
              i18n="
                When a workspace last changed. TIME is a relative time such as
                '2 days ago'
              ">
              Updated
              {{
                workspace().updatedAt | fromNow // i18n(ph="TIME")
              }}
            </ng-container>
          </span>
        </span>
      </span>

      <span class="relative flex shrink-0 items-center gap-1">
        <button
          type="button"
          class="pin-button focus-visible:outline-primary inline-flex h-8 w-8 cursor-pointer items-center justify-center rounded-lg bg-transparent transition-colors focus-visible:outline-2 focus-visible:outline-offset-2"
          [class.pinned]="isPinned()"
          [attr.aria-pressed]="isPinned()"
          [attr.aria-label]="pinLabel()"
          [title]="pinLabel()"
          (click)="onPinClicked($event)">
          <svg
            lucidePin
            class="h-4 w-4"
            [class.fill-current]="isPinned()"
            aria-hidden="true"></svg>
        </button>
        <span
          aria-hidden="true"
          class="inline-flex h-8 w-6.5 items-center justify-center text-[rgba(var(--foreground-rgb),0.32)]">
          <svg lucideChevronRight class="h-4.25 w-4.25"></svg>
        </span>
      </span>
    </div>
  `,
})
export class WorkspaceListItemComponent {
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly theme = inject(ThemeService).theme;

  readonly workspace = input.required<Workspace>();
  readonly isPinned = input(false);

  readonly open = output();
  readonly pinToggle = output();

  protected readonly letter = computed(() => {
    return this.workspace().name.trim().charAt(0) || '?';
  });

  protected readonly badgeColor = computed(() => {
    return colorHex(this.workspace().metaInfo?.color);
  });

  protected readonly badgeGlow = computed(() => {
    return `0 4px 14px ${this.badgeColor()}38`;
  });

  protected readonly logoUrl = computed(() => {
    const workspace = this.workspace();

    return brandingImageUrl(workspace.slug, workspace.metaInfo?.logoFileId);
  });

  protected readonly memberCount = computed(() => {
    return this.workspace().memberCount ?? 0;
  });

  protected readonly avatars = computed<RowAvatar[]>(() => {
    const members = this.workspace().members ?? [];

    return members.slice(0, maxAvatars).map((member, index) => ({
      id: member.id,
      initials: initialsOf(member.displayName),
      color: avatarColors[index % avatarColors.length],
    }));
  });

  protected readonly pinLabel = computed(() => {
    return this.isPinned()
      ? $localize`:Button that removes a workspace from the top of the list:Unpin workspace`
      : $localize`:Button that pins a workspace to the top of the list:Pin workspace`;
  });

  constructor() {
    effect(() => {
      const variables = workspaceBrandVariables(
        this.workspace().metaInfo?.color,
        this.theme() === 'dark'
      );

      const style = this.elementRef.nativeElement.style;

      for (const [property, value] of Object.entries(variables)) {
        if (value) {
          style.setProperty(property, value);
        } else {
          style.removeProperty(property);
        }
      }
    });
  }

  protected onNameClicked(event: MouseEvent) {
    event.stopPropagation();
    this.open.emit();
  }

  protected onPinClicked(event: MouseEvent) {
    event.stopPropagation();
    this.pinToggle.emit();
  }
}

function initialsOf(displayName: string): string {
  const words = displayName
    .split(' ')
    .map((word) => word.trim())
    .filter((word) => !!word);

  if (!words.length) return '?';
  if (words.length === 1) return words[0][0].toUpperCase();

  return `${words[0][0]}${words[1][0]}`.toUpperCase();
}
