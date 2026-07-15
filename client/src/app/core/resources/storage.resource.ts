import { httpResource } from '@angular/common/http';
import { ClientResponse } from '../models/client-response';
import { WorkspaceStorageUsage } from '../models/view-models/workspace-file-view-model';

export const storageUsageResource = () => {
  return httpResource<ClientResponse<WorkspaceStorageUsage>>(
    () => 'api/storage/usage'
  );
};
