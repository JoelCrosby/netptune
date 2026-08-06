export enum ImportStage {
  uploaded = 0,
  inspected = 1,
  mapped = 2,
  previewed = 3,
  committing = 4,
  committed = 5,
  failed = 6,
  undone = 7,
  abandoned = 8,
}

export enum ImportDiagnosticSeverity {
  error = 0,
  warning = 1,
  info = 2,
}

export enum ImportRowAction {
  create = 0,
  update = 1,
  skip = 2,
  error = 3,
}

export interface ImportSessionViewModel {
  publicId: string;
  stage: ImportStage;
  sourceKind: number;
  vendorProfile: number;
  originalName: string;
  sizeBytes: number;
  targetRecordType: string;
  targetProjectKey?: string;
  targetBoardIdentifier?: string;
  progressPercent: number;
  progressMessage?: string;
  error?: string;
  created: number;
  updated: number;
  skipped: number;
  failed: number;
  canUndo: boolean;
  createdByUserId?: string;
  createdByDisplayName?: string;
  createdAt: string;
  committedAt?: string;
  expiresAt: string;
}

export interface ImportSessionState {
  session: ImportSessionViewModel;
  sourceProfile?: ImportSourceProfile;
  mapping?: ImportMappingModel;
  previewResult?: ImportPreviewResult;
}

export interface ImportSourceColumn {
  index: number;
  name: string;
  inferredType: number;
  nonEmptyCount: number;
  distinctCount: number;
  sampleValues: string[];
}

export interface ImportSourceProfile {
  kind: number;
  encoding?: string;
  delimiter?: string;
  hasHeaderRow: boolean;
  sheetNames: string[];
  selectedSheet?: string;
  vendorProfile?: string;
  estimatedRowCount: number;
  columns: ImportSourceColumn[];
}

export interface ImportFieldBinding {
  fieldKey: string;
  columnIndex?: number | null;
  constant?: string | null;
  transforms: { kind: number; argument?: string }[];
  valueMap: Record<string, string>;
  confidence: number;
  origin: number;
}

export interface ImportMappingModel {
  recordType: string;
  bindings: ImportFieldBinding[];
  dedupe?: { keyFieldKey: string; action: number } | null;
}

export interface ImportRowDiagnostic {
  rowNumber: number;
  columnName?: string;
  severity: ImportDiagnosticSeverity;
  code: string;
  message: string;
  value?: string;
}

export interface ImportRowPreview {
  rowNumber: number;
  action: ImportRowAction;
  matchedRef?: string;
  resolved: Record<string, string | null>;
}

export interface ImportPreviewResult {
  totalRows: number;
  willCreate: number;
  willUpdate: number;
  willSkip: number;
  willError: number;
  isExtrapolated: boolean;
  diagnostics: ImportRowDiagnostic[];
  newEntities: { entityType: string; name: string }[];
  usersToInvite: string[];
  sampleRows: ImportRowPreview[];
}

export interface ImportMappingSuggestion {
  mapping: ImportMappingModel;
  vendor: number;
  vendorConfidence: number;
  unmappedColumns: number[];
}

export interface ImproveImportMappingResult {
  mapping: ImportMappingModel;
  discardedBindings: number;
  discardReasons: string[];
  notes?: string;
  usedDataSampling: boolean;
}

export interface ImportUndoResult {
  reverted: number;
  blocked: string[];
}
