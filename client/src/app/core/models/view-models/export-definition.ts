import { ExportFormat } from './export-job-view-model';

export interface ExportFilterModel {
  projectKeys: string[];
  boardIdentifiers: string[];
  statusKeys: string[];
  statusCategories: number[];
  tags: string[];
  assigneeEmails: string[];
  priorities: number[];
  sprintRef?: string | null;
  term?: string | null;
  includeDeleted: boolean;
  createdFrom?: string | null;
  createdTo?: string | null;
  updatedSince?: string | null;
}

export interface ExportOptionsModel {
  delimiter: string;
  dateFormat: string;
  timeZoneId: string;
  collectionSeparator: string;
  includeHeaderRow: boolean;
  expandCollectionsToRows: boolean;
  includeHistory: boolean;
  includeFiles: boolean;
  includeMembers: boolean;
}

export interface ExportDefinitionModel {
  recordType: string;
  format: ExportFormat;
  fields: string[];
  filter?: ExportFilterModel | null;
  options: ExportOptionsModel;
}

export interface ExportDefinitionViewModel {
  id: number;
  name: string;
  description?: string;
  recordType: string;
  format: ExportFormat;
  isShared: boolean;
  definition?: ExportDefinitionModel;
  createdByUserId?: string;
  createdByDisplayName?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface ExportPreviewResult {
  fieldKeys: string[];
  headers: string[];
  rows: string[][];
  estimatedRowCount: number;
  canRunInline: boolean;
  archiveFileBytes: number;
}

export interface TransferField {
  key: string;
  name: string;
  valueType: number;
  isCollection: boolean;
  isRequiredForImport: boolean;
  isExportedByDefault: boolean;
  refType?: string;
  synonyms: string[];
  example?: string;
}

export interface TransferRecordType {
  key: string;
  name: string;
  isStandaloneExportable: boolean;
  fields: TransferField[];
}

export interface TransferCatalog {
  recordTypes: TransferRecordType[];
}

export function defaultExportOptions(): ExportOptionsModel {
  return {
    delimiter: ',',
    dateFormat: 'yyyy-MM-dd',
    timeZoneId: 'UTC',
    collectionSeparator: '|',
    includeHeaderRow: true,
    expandCollectionsToRows: false,
    includeHistory: false,
    includeFiles: false,
    includeMembers: false,
  };
}

export function emptyExportFilter(): ExportFilterModel {
  return {
    projectKeys: [],
    boardIdentifiers: [],
    statusKeys: [],
    statusCategories: [],
    tags: [],
    assigneeEmails: [],
    priorities: [],
    sprintRef: null,
    term: null,
    includeDeleted: false,
    createdFrom: null,
    createdTo: null,
    updatedSince: null,
  };
}
