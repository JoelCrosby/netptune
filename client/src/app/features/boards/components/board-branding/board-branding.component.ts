import {
  Component,
  computed,
  inject,
  input,
  linkedSignal,
} from '@angular/core';
import { BrandingTarget } from '@core/models/branding';
import { BrandingCommandsService } from '@core/services/branding-commands.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { brandingImageUrl } from '@core/util/branding';
import { ImageUploadComponent } from '@static/components/image-upload/image-upload.component';

@Component({
  selector: 'app-board-branding',
  imports: [ImageUploadComponent],
  host: { class: 'block' },
  template: `
    <div class="flex flex-col gap-6">
      <div>
        <p class="mb-2 text-sm font-medium">
          <span i18n="Label of the board logo picker">Board Logo</span>
        </p>
        <app-image-upload
          [imageUrl]="logoUrl()"
          i18n-alt="Alt text for the board logo preview"
          alt="Board logo"
          [uploading]="branding.isUploading()"
          (fileSelected)="onLogoSelected($event)"
          (removed)="onLogoRemoved()" />
      </div>

      <div>
        <p class="mb-2 text-sm font-medium">
          <span i18n="Label of the board background image picker">
            Board Background
          </span>
        </p>
        <p
          class="text-muted mb-2 text-xs"
          i18n="Explains where the board background image appears">
          Fills the background of this board's page.
        </p>
        <app-image-upload
          shape="wide"
          [imageUrl]="backgroundUrl()"
          i18n-alt="Alt text for the board background image preview"
          alt="Board background"
          [uploading]="branding.isUploading()"
          (fileSelected)="onBackgroundSelected($event)"
          (removed)="onBackgroundRemoved()" />
      </div>
    </div>
  `,
})
export class BoardBrandingComponent {
  readonly boardId = input.required<number>();
  readonly initialLogoFileId = input<string | null>(null);
  readonly initialBackgroundFileId = input<string | null>(null);

  protected readonly branding = inject(BrandingCommandsService);

  private readonly workspaceSlug = inject(CurrentWorkspaceService).slug;

  private readonly logoFileId = linkedSignal(() => this.initialLogoFileId());
  private readonly backgroundFileId = linkedSignal(() => {
    return this.initialBackgroundFileId();
  });

  protected readonly logoUrl = computed(() => {
    return brandingImageUrl(this.workspaceSlug(), this.logoFileId());
  });

  protected readonly backgroundUrl = computed(() => {
    return brandingImageUrl(this.workspaceSlug(), this.backgroundFileId());
  });

  private readonly logoTarget = computed<BrandingTarget>(() => {
    return { kind: 'boardLogo', boardId: this.boardId() };
  });

  private readonly backgroundTarget = computed<BrandingTarget>(() => {
    return { kind: 'boardBackground', boardId: this.boardId() };
  });

  protected onLogoSelected(file: File) {
    this.branding.upload(this.logoTarget(), file, (fileId) => {
      this.logoFileId.set(fileId);
    });
  }

  protected onLogoRemoved() {
    this.branding.remove(this.logoTarget(), (fileId) => {
      this.logoFileId.set(fileId);
    });
  }

  protected onBackgroundSelected(file: File) {
    this.branding.upload(this.backgroundTarget(), file, (fileId) => {
      this.backgroundFileId.set(fileId);
    });
  }

  protected onBackgroundRemoved() {
    this.branding.remove(this.backgroundTarget(), (fileId) => {
      this.backgroundFileId.set(fileId);
    });
  }
}
