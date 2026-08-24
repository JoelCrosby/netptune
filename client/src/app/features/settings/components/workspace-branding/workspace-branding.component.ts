import { Component, computed, inject } from '@angular/core';
import { BrandingTarget } from '@core/models/branding';
import { BrandingCommandsService } from '@core/services/branding-commands.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { brandingImageUrl } from '@core/util/branding';
import {
  FormControlHintDirective,
  FormControlLabelDirective,
} from '@static/components/form-control/form-control.directives';
import { ImageUploadComponent } from '@static/components/image-upload/image-upload.component';

@Component({
  selector: 'app-workspace-branding',
  imports: [
    FormControlHintDirective,
    FormControlLabelDirective,
    ImageUploadComponent,
  ],
  host: { class: 'block' },
  template: `
    <p appFormLabel i18n="Label of the workspace logo picker">Workspace Logo</p>

    <app-image-upload
      [imageUrl]="logoUrl()"
      i18n-alt="Alt text for the workspace logo preview"
      alt="Workspace logo"
      [uploading]="branding.isUploading()"
      (fileSelected)="onFileSelected($event)"
      (removed)="onRemove()" />

    <p appFormHint i18n="Explains where the workspace logo appears">
      Shown beside the workspace name in the sidebar and the workspace switcher.
      Counts towards this workspace's storage allowance.
    </p>
  `,
})
export class WorkspaceBrandingComponent {
  protected readonly branding = inject(BrandingCommandsService);

  private readonly currentWorkspace = inject(CurrentWorkspaceService);

  protected readonly logoUrl = computed(() => {
    const workspace = this.currentWorkspace.workspace();

    return brandingImageUrl(workspace?.slug, workspace?.metaInfo?.logoFileId);
  });

  private readonly target: BrandingTarget = { kind: 'workspaceLogo' };

  protected onFileSelected(file: File) {
    this.branding.upload(this.target, file);
  }

  protected onRemove() {
    this.branding.remove(this.target);
  }
}
