import { httpResource } from '@angular/common/http';
import { computed, Injectable } from '@angular/core';
import { WorkspaceSetupTemplate } from '@core/models/workspace-setup-template';

@Injectable({ providedIn: 'root' })
export class WorkspaceSetupTemplatesService {
  private readonly resource = httpResource<WorkspaceSetupTemplate[]>(
    () => 'api/setup-templates'
  );

  readonly templates = computed(() => this.resource.value() ?? []);
  readonly loading = this.resource.isLoading;
  readonly error = this.resource.error;
}
