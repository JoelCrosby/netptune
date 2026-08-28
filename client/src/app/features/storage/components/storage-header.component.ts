import { Component, computed } from '@angular/core';
import { storageUsageResource } from '@core/resources/storage.resource';
import { LucideHardDrive } from '@lucide/angular';
import {
  BadgeColor,
  BadgeComponent,
} from '@static/components/badge/badge.component';
import { IconTileComponent } from '@static/components/icon-tile.component';
import {
  ProgressBarColor,
  ProgressBarComponent,
} from '@static/components/progress-bar/progress-bar.component';
import { SkeletonComponent } from '@static/components/skeleton/skeleton.component';
import {
  StatStripComponent,
  StatStripItem,
} from '@static/components/stat-strip/stat-strip.component';
import { FileSizePipe } from '@static/pipes/file-size.pipe';
import { formatBytes } from '@core/util/bytes';

const overLimitThreshold = 100;
const nearLimitThreshold = 80;

@Component({
  selector: 'app-storage-header',
  imports: [
    BadgeComponent,
    FileSizePipe,
    IconTileComponent,
    ProgressBarComponent,
    SkeletonComponent,
    StatStripComponent,
  ],
  template: `
    @if (usage(); as usage) {
      <section
        class="border-border bg-card mb-6 overflow-hidden rounded-lg border shadow-sm">
        <header
          class="border-border flex flex-wrap items-start justify-between gap-x-4 gap-y-3 border-b px-6 py-5">
          <div class="flex min-w-0 items-start gap-3">
            <app-icon-tile [icon]="storageIcon" />

            <div class="min-w-0">
              <h2
                class="font-overpass text-base font-semibold"
                i18n="Heading of the storage usage summary">
                Storage usage
              </h2>
              <p
                class="text-muted mt-1 text-sm"
                i18n="Explains what is excluded from the storage total">
                Profile pictures and audit archives are excluded from workspace
                usage.
              </p>
            </div>
          </div>

          <app-badge
            [color]="badgeColor()"
            shape="rounded"
            class="px-2.5 py-1 tabular-nums">
            {{ percentageLabel() }}
          </app-badge>
        </header>

        <div class="px-6 py-5">
          <p class="flex flex-wrap items-baseline gap-x-2">
            <span class="text-3xl font-semibold tracking-tight tabular-nums">
              {{ usage.usedBytes | fileSize }}
            </span>
            <span
              class="text-muted text-sm"
              i18n="
                Storage limit shown next to the used total. LIMIT is a formatted
                byte count
              ">
              of
              {{
                usage.limitBytes | fileSize // i18n(ph="LIMIT")
              }}
            </span>
          </p>

          <app-progress-bar
            class="mt-4 h-2"
            [value]="percentage()"
            [color]="progressColor()" />
        </div>

        <app-stat-strip [items]="stats()" />
      </section>
    } @else if (loading()) {
      <section
        class="border-border bg-card mb-6 rounded-lg border p-6 shadow-sm"
        role="status"
        i18n-aria-label="Accessible label while the storage summary loads"
        aria-label="Loading storage usage">
        <app-skeleton class="h-4 w-40" />
        <app-skeleton class="mt-4 h-8 w-56" />
        <app-skeleton class="mt-4 h-2 w-full rounded-full" />

        <div class="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-3">
          @for (tile of skeletonTiles; track $index) {
            <app-skeleton class="h-10" />
          }
        </div>
      </section>
    }
  `,
})
export class StorageHeaderComponent {
  private readonly usageResource = storageUsageResource();

  protected readonly storageIcon = LucideHardDrive;
  protected readonly skeletonTiles = Array.from({ length: 3 });

  protected readonly usage = computed(() => {
    return this.usageResource.value()?.payload ?? null;
  });

  protected readonly loading = computed(() => this.usageResource.isLoading());

  protected readonly percentage = computed(() => {
    return Math.min(overLimitThreshold, this.usage()?.percentage ?? 0);
  });

  protected readonly stats = computed<StatStripItem[]>(() => {
    const usage = this.usage();

    if (!usage) return [];

    const averageBytes = usage.fileCount
      ? Math.round(usage.usedBytes / usage.fileCount)
      : 0;

    return [
      {
        label: $localize`:Label for the remaining storage figure:Available`,
        value: formatBytes(usage.availableBytes),
      },
      {
        label: $localize`:Label for the number of files counted towards storage:Tracked files`,
        value: usage.fileCount,
      },
      {
        label: $localize`:Label for the mean size of the tracked files:Average file size`,
        value: formatBytes(averageBytes),
      },
    ];
  });

  protected readonly percentageLabel = computed(() => {
    const percentage = Math.round(this.usage()?.percentage ?? 0);

    return $localize`:Share of the storage limit currently in use:${percentage}:PERCENT: used`;
  });

  protected readonly progressColor = computed<ProgressBarColor>(() => {
    const percentage = this.percentage();

    if (percentage >= overLimitThreshold) return 'destructive';
    if (percentage >= nearLimitThreshold) return 'warn';

    return 'primary';
  });

  protected readonly badgeColor = computed<BadgeColor>(() => {
    const percentage = this.percentage();

    if (percentage >= overLimitThreshold) return 'warn';
    if (percentage >= nearLimitThreshold) return 'pending';

    return 'neutral';
  });

  reload() {
    this.usageResource.reload();
  }
}
