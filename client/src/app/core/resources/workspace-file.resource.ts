import { httpResource } from '@angular/common/http';
import { Signal } from '@angular/core';
import { ClientResponse } from '../models/client-response';
import { WorkspaceFileViewModel } from '../models/view-models/workspace-file-view-model';

export const taskFilesResource = (systemId: Signal<string>) => {
  return httpResource<ClientResponse<WorkspaceFileViewModel[]>>(() => {
    return `api/tasks/${encodeURIComponent(systemId())}/files`;
  });
};
