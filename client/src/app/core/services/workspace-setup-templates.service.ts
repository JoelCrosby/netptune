import { httpResource } from '@angular/common/http';
import { computed, Service } from '@angular/core';
import { WorkspaceSetupTemplate } from '@core/models/workspace-setup-template';

@Service()
export class WorkspaceSetupTemplatesService {
  private readonly resource = httpResource<WorkspaceSetupTemplate[]>(
    () => 'api/setup-templates'
  );

  readonly templates = computed(() => this.resource.value() ?? []);
  readonly loading = this.resource.isLoading;
  readonly error = this.resource.error;
}
