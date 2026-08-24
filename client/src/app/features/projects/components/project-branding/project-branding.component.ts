import { Component, computed, inject, input } from '@angular/core';
import { BrandingTarget } from '@core/models/branding';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { BrandingCommandsService } from '@core/services/branding-commands.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { brandingImageUrl } from '@core/util/branding';
import {
  FormControlHintDirective,
  FormControlLabelDirective,
} from '@static/components/form-control/form-control.directives';
import { ImageUploadComponent } from '@static/components/image-upload/image-upload.component';

@Component({
  selector: 'app-project-branding',
  imports: [
    FormControlHintDirective,
    FormControlLabelDirective,
    ImageUploadComponent,
  ],
  host: { class: 'block' },
  template: `
    <p appFormLabel i18n="Label of the project logo picker">Project Logo</p>

    <app-image-upload
      [imageUrl]="logoUrl()"
      i18n-alt="Alt text for the project logo preview"
      alt="Project logo"
      [uploading]="branding.isUploading()"
      (fileSelected)="onFileSelected($event)"
      (removed)="onRemove()" />

    <p appFormHint i18n="Explains where the project logo appears">
      Shown beside the project wherever it is listed. Counts towards this
      workspace's storage allowance.
    </p>
  `,
})
export class ProjectBrandingComponent {
  readonly project = input<ProjectViewModel>();

  protected readonly branding = inject(BrandingCommandsService);

  private readonly workspaceSlug = inject(CurrentWorkspaceService).slug;

  protected readonly logoUrl = computed(() => {
    return brandingImageUrl(this.workspaceSlug(), this.project()?.logoFileId);
  });

  private readonly target = computed<BrandingTarget | null>(() => {
    const projectId = this.project()?.id;

    if (projectId === undefined) return null;

    return { kind: 'projectLogo', projectId };
  });

  protected onFileSelected(file: File) {
    const target = this.target();

    if (!target) return;

    this.branding.upload(target, file);
  }

  protected onRemove() {
    const target = this.target();

    if (!target) return;

    this.branding.remove(target);
  }
}
