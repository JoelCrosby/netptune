import { httpResource } from '@angular/common/http';
import { PERMISSONS } from '../auth/permissions';
import {
  ExportDefinitionViewModel,
  TransferCatalog,
} from '../models/view-models/export-definition';
import { permissionResource } from './permission-resource';

export const transferCatalogResource = () => {
  return httpResource<TransferCatalog>(() => 'api/transfer/catalog', {
    defaultValue: { recordTypes: [] },
  });
};

export const exportDefinitionResource = () => {
  return permissionResource<ExportDefinitionViewModel[]>(
    PERMISSONS.tasks.export,
    () => ({ url: 'api/export/definitions' }),
    { defaultValue: [] }
  );
};
