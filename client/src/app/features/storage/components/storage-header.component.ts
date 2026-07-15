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
    <app-page-header title="Storage" />

    @if (usage(); as usage) {
      <app-card class="mb-6 block">
        <app-card-header>
          <app-card-title>
            {{ usage.usedBytes | fileSize }} of
            {{ usage.limitBytes | fileSize }} used
          </app-card-title>
          <app-card-subtitle>
            {{ usage.fileCount }} tracked files ·
            {{ usage.availableBytes | fileSize }} available
          </app-card-subtitle>
        </app-card-header>

        <app-card-content>
          <app-progress-bar
            class="h-3"
            [value]="percentage()"
            [color]="progressColor()" />
          <p class="text-muted text-xs">
            Profile pictures and audit archives are excluded from workspace
            usage.
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
