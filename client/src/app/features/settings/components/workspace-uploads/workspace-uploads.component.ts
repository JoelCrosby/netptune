import { Component, computed, inject } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { WorkspaceCommandsService } from '@core/services/workspace-commands.service';
import { formatBytes } from '@core/util/bytes';
import { maxUploadPresets } from '@core/util/upload-limits';
import { LucideUpload } from '@lucide/angular';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { IconTileComponent } from '@static/components/icon-tile.component';

@Component({
  selector: 'app-workspace-uploads',
  imports: [FormSelectComponent, FormSelectOptionComponent, IconTileComponent],
  host: { class: 'block' },
  template: `
    <section
      class="border-border bg-card overflow-hidden rounded-lg border shadow-sm">
      <header class="border-border border-b px-6 py-5">
        <div class="flex min-w-0 items-center gap-3">
          <app-icon-tile [icon]="uploadIcon" />

          <div class="min-w-0">
            <h2
              class="font-overpass text-base font-semibold"
              i18n="Section heading for the workspace upload settings">
              Uploads
            </h2>
            <p
              class="text-muted mt-1 text-sm"
              i18n="Explains what the maximum upload size controls">
              The largest single file anyone in this workspace can attach to a
              task or embed in a description. Larger files are rejected before
              they are stored.
            </p>
          </div>
        </div>
      </header>

      <div class="max-w-sm px-6 py-5">
        <app-form-select
          name="workspace-max-upload"
          i18n-label="Label of the maximum upload size field"
          label="Maximum file size"
          [noMargin]="true"
          [disabled]="!canUpdate() || saving()"
          [value]="maxUploadBytes()"
          (changed)="save($event)">
          @for (option of options(); track option) {
            <app-form-select-option [value]="option">
              {{ formatBytes(option) }}
            </app-form-select-option>
          }
        </app-form-select>
      </div>
    </section>
  `,
})
export class WorkspaceUploadsComponent {
  private readonly workspaceCommands = inject(WorkspaceCommandsService);
  private readonly currentWorkspace = inject(CurrentWorkspaceService);

  protected readonly formatBytes = formatBytes;
  protected readonly uploadIcon = LucideUpload;

  readonly canUpdate = hasPermission(PERMISSIONS.workspace.update);
  readonly saving = this.workspaceCommands.editLoading;
  readonly maxUploadBytes = this.currentWorkspace.maxUploadBytes;

  // A workspace configured through the API can sit between the presets, and the select shows
  // nothing at all when its value has no matching option.
  readonly options = computed(() => {
    const values = new Set([...maxUploadPresets, this.maxUploadBytes()]);

    return [...values].sort((first, second) => first - second);
  });

  save(maxUploadBytes: number) {
    const workspace = this.currentWorkspace.workspace();
    const unchanged = maxUploadBytes === this.maxUploadBytes();

    if (!workspace?.slug || unchanged) return;

    this.workspaceCommands.edit({
      slug: workspace.slug,
      metaInfo: workspace.metaInfo ?? {},
      maxUploadBytes,
    });
  }
}
