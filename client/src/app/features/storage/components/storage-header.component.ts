import { Component, computed } from '@angular/core';
import { storageUsageResource } from '@core/resources/storage.resource';
import { LucideHardDrive } from '@lucide/angular';
import {
  BadgeColor,
  BadgeComponent,
} from '@static/components/badge/badge.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import {
  ProgressBarColor,
  ProgressBarComponent,
} from '@static/components/progress-bar/progress-bar.component';
import { SkeletonComponent } from '@static/components/skeleton/skeleton.component';
import { FileSizePipe } from '@static/pipes/file-size.pipe';

const overLimitThreshold = 100;
const nearLimitThreshold = 80;

@Component({
  selector: 'app-storage-header',
  imports: [
    BadgeComponent,
    FileSizePipe,
    LucideHardDrive,
    PageHeaderComponent,
    ProgressBarComponent,
    SkeletonComponent,
  ],
  template: `
    <app-page-header
      i18n-title="Page title for workspace file storage"
      title="Storage" />

    @if (usage(); as usage) {
      <section
        class="border-border bg-card mb-6 overflow-hidden rounded-lg border shadow-sm">
        <header
          class="border-border flex flex-wrap items-start justify-between gap-x-4 gap-y-3 border-b px-6 py-5">
          <div class="flex min-w-0 items-start gap-3">
            <span
              class="bg-primary/10 text-primary flex h-9 w-9 shrink-0 items-center justify-center rounded-lg"
              aria-hidden="true">
              <svg lucideHardDrive class="h-4 w-4"></svg>
            </span>

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

        <dl
          class="border-border divide-border grid grid-cols-1 divide-y border-t sm:grid-cols-3 sm:divide-x sm:divide-y-0">
          <div class="px-6 py-4">
            <dt
              class="text-muted text-xs font-medium tracking-wide uppercase"
              i18n="Label for the remaining storage figure">
              Available
            </dt>
            <dd class="mt-1 text-lg font-semibold tabular-nums">
              {{ usage.availableBytes | fileSize }}
            </dd>
          </div>

          <div class="px-6 py-4">
            <dt
              class="text-muted text-xs font-medium tracking-wide uppercase"
              i18n="Label for the number of files counted towards storage">
              Tracked files
            </dt>
            <dd class="mt-1 text-lg font-semibold tabular-nums">
              {{ usage.fileCount }}
            </dd>
          </div>

          <div class="px-6 py-4">
            <dt
              class="text-muted text-xs font-medium tracking-wide uppercase"
              i18n="Label for the mean size of the tracked files">
              Average file size
            </dt>
            <dd class="mt-1 text-lg font-semibold tabular-nums">
              {{ averageFileSize() | fileSize }}
            </dd>
          </div>
        </dl>
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

  protected readonly skeletonTiles = Array.from({ length: 3 });

  protected readonly usage = computed(() => {
    return this.usageResource.value()?.payload ?? null;
  });

  protected readonly loading = computed(() => this.usageResource.isLoading());

  protected readonly percentage = computed(() => {
    return Math.min(overLimitThreshold, this.usage()?.percentage ?? 0);
  });

  protected readonly averageFileSize = computed(() => {
    const usage = this.usage();

    if (!usage?.fileCount) return 0;

    return Math.round(usage.usedBytes / usage.fileCount);
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
