export interface HeaderValidationResult {
  isSuccess: boolean;
  invalidHeaders?: string[];
  missingHeaders?: string[];
}

export interface TaskImportResult {
  headerValidationResult?: HeaderValidationResult;
  missingEmails?: string[];
}
