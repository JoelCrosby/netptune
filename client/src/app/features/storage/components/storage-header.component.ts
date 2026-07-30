import { Component, computed } from '@angular/core';
import { storageUsageResource } from '@core/resources/storage.resource';
import { CardContentComponent } from '@static/components/card/card-content.component';
import { CardHeaderComponent } from '@static/components/card/card-header.component';
import { CardSubtitleComponent } from '@static/components/card/card-subtitle.component';
import { CardTitleComponent } from '@static/components/card/card-title.component';
import { CardComponent } from '@static/components/card/card.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import {
  ProgressBarColor,
  ProgressBarComponent,
} from '@static/components/progress-bar/progress-bar.component';
import { FileSizePipe } from '../pipes/file-size.pipe';

@Component({
  selector: 'app-storage-header',
  imports: [
    CardComponent,
    CardContentComponent,
    CardHeaderComponent,
    CardSubtitleComponent,
    CardTitleComponent,
    FileSizePipe,
    PageHeaderComponent,
    ProgressBarComponent,
  ],
  template: `
    <app-page-header
      i18n-title="Page title for workspace file storage"
      title="Storage" />

    @if (usage(); as usage) {
      <app-card class="mb-6 block">
        <app-card-header>
          <app-card-title>
            <span
              i18n="Storage usage. USED and LIMIT are formatted byte counts">
              {{
                usage.usedBytes | fileSize // i18n(ph="USED")
              }}
              of
              {{
                usage.limitBytes | fileSize // i18n(ph="LIMIT")
              }}
              used
            </span>
          </app-card-title>
          <app-card-subtitle>
            <span
              i18n="
                File count and remaining storage. COUNT is the number of files
                and AVAILABLE a formatted byte count
              ">
              {{
                usage.fileCount // i18n(ph="COUNT")
              }}
              tracked files ·
              {{
                usage.availableBytes | fileSize // i18n(ph="AVAILABLE")
              }}
              available
            </span>
          </app-card-subtitle>
        </app-card-header>

        <app-card-content>
          <app-progress-bar
            class="h-3"
            [value]="percentage()"
            [color]="progressColor()" />
          <p class="text-muted text-xs">
            <span i18n="Explains what is excluded from the storage total">
              Profile pictures and audit archives are excluded from workspace
              usage.
            </span>
          </p>
        </app-card-content>
      </app-card>
    }
  `,
})
export class StorageHeaderComponent {
  private readonly usageResource = storageUsageResource();

  protected readonly usage = computed(() => {
    return this.usageResource.value()?.payload ?? null;
  });

  protected readonly percentage = computed(() => {
    return Math.min(100, this.usage()?.percentage ?? 0);
  });

  protected readonly progressColor = computed<ProgressBarColor>(() => {
    const percentage = this.percentage();

    if (percentage >= 100) return 'destructive';
    if (percentage >= 80) return 'warn';

    return 'primary';
  });

  reload() {
    this.usageResource.reload();
  }
}
