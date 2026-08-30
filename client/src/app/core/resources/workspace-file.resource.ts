import { httpResource } from '@angular/common/http';
import { Signal } from '@angular/core';
import { ClientResponse } from '../models/client-response';
import { WorkspaceFileViewModel } from '../models/view-models/workspace-file-view-model';

export const taskFilesResource = (systemId: Signal<string | undefined>) => {
  return httpResource<ClientResponse<WorkspaceFileViewModel[]>>(() => {
    const id = systemId();

    if (!id) return undefined;

    return `api/tasks/${encodeURIComponent(id)}/files`;
  });
};
