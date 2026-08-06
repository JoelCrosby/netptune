import {
  defaultExportOptions,
  emptyExportFilter,
  ExportDefinitionModel,
} from '@core/models/view-models/export-definition';
import { ExportFormat } from '@core/models/view-models/export-job-view-model';

/**
 * The canned CSV definition behind the board and task-list "Export tasks" menu
 * items, kept row-expanded so the file matches what those menus produced before
 * exports became configurable.
 */
export function taskExportDefinition(
  boardIdentifier?: string
): ExportDefinitionModel {
  return {
    recordType: 'task',
    format: ExportFormat.csv,
    fields: [],
    filter: {
      ...emptyExportFilter(),
      boardIdentifiers: boardIdentifier ? [boardIdentifier] : [],
    },
    options: { ...defaultExportOptions(), expandCollectionsToRows: true },
  };
}
