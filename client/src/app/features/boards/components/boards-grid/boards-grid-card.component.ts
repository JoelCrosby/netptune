import { Component, computed, inject, input } from '@angular/core';
import { BoardViewModel } from '@core/models/view-models/board-view-model';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { brandingImageUrl } from '@core/util/branding';
import { colorBackgroundClass } from '@core/util/colors/colors';
import { LucideChartColumnBig } from '@lucide/angular';
import { IconTileComponent } from '@static/components/icon-tile.component';
import { FromNowPipe } from '@static/pipes/from-now.pipe';

interface BoardStat {
  label: string;
  value: string | number;
}

@Component({
  selector: 'app-boards-grid-card',
  providers: [FromNowPipe],
  imports: [IconTileComponent],
  host: { class: 'block h-full' },
  template: `
    <article
      class="border-border bg-card hover:border-primary/40 flex h-full min-h-38 flex-col overflow-hidden rounded-lg border shadow-sm transition-colors">
      <div class="flex flex-1 items-start gap-3 px-5 py-4">
        @if (logoUrl(); as url) {
          <img
            [src]="url"
            [alt]="board().name"
            class="border-border h-9 w-9 shrink-0 rounded-lg border object-cover" />
        } @else {
          <app-icon-tile [icon]="boardIcon" [class]="tileClass()" />
        }

        <h3 class="font-overpass min-w-0 truncate text-base font-semibold">
          {{ board().name }}
        </h3>
      </div>

      <dl
        class="border-border divide-border grid grid-cols-2 divide-x border-t">
        @for (stat of stats(); track stat.label) {
          <div class="min-w-0 px-5 py-3">
            <dt
              class="text-muted truncate text-[0.7rem] font-medium tracking-wide uppercase">
              {{ stat.label }}
            </dt>
            <dd class="mt-0.5 truncate text-sm font-semibold">
              {{ stat.value }}
            </dd>
          </div>
        }
      </dl>
    </article>
  `,
})
export class BoardsGridCardComponent {
  readonly board = input.required<BoardViewModel>();

  private readonly fromNow = inject(FromNowPipe);
  private readonly workspaceSlug = inject(CurrentWorkspaceService).slug;

  protected readonly logoUrl = computed(() => {
    return brandingImageUrl(
      this.workspaceSlug(),
      this.board().metaInfo?.logoFileId
    );
  });

  protected readonly boardIcon = LucideChartColumnBig;

  protected readonly tileClass = computed(() => {
    return `${colorBackgroundClass(this.board().metaInfo.color)} text-white`;
  });

  protected readonly stats = computed<BoardStat[]>(() => {
    const board = this.board();

    return [
      {
        label: $localize`:Stat label for the number of tasks on a board:Tasks`,
        value: board.taskCount,
      },
      {
        label: $localize`:Stat label for when a board was last changed:Modified`,
        value: this.fromNow.transform(board.lastUpdated),
      },
    ];
  });
}
