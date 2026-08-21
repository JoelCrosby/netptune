import { computed, effect, Injectable, signal } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import {
  defaultExportOptions,
  emptyExportFilter,
  ExportDefinitionModel,
  ExportDefinitionViewModel,
  ExportFilterModel,
  ExportOptionsModel,
  TransferField,
} from '@core/models/view-models/export-definition';
import { ExportFormat } from '@core/models/view-models/export-job-view-model';
import {
  exportDefinitionResource,
  transferCatalogResource,
} from '@core/resources/transfer.resource';

export const ExportLastStepIndex = 4;

export type ExportFilterListKey =
  'projectKeys' | 'boardIdentifiers' | 'statusKeys' | 'tags';

@Injectable()
export class ExportWizardService {
  readonly recordType = signal('task');
  readonly format = signal(ExportFormat.csv);
  readonly fields = signal<string[]>([]);
  readonly filter = signal<ExportFilterModel>(emptyExportFilter());
  readonly options = signal<ExportOptionsModel>(defaultExportOptions());

  readonly activeIndex = signal(0);

  readonly catalog = transferCatalogResource();
  readonly savedDefinitions = exportDefinitionResource();

  readonly canExportArchive = hasPermission(PERMISSIONS.data.exportArchive);

  readonly isArchive = computed(() => this.format() === ExportFormat.archive);

  readonly visibleDefinitions = computed(() => {
    const saved = this.savedDefinitions.value();

    if (this.canExportArchive()) {
      return saved;
    }

    return saved.filter(
      (definition) => definition.definition?.format !== ExportFormat.archive
    );
  });

  readonly definition = computed<ExportDefinitionModel>(() => {
    return {
      recordType: this.recordType(),
      format: this.format(),
      fields: this.isArchive() ? [] : this.fields(),
      filter: this.filter(),
      options: this.options(),
    };
  });

  readonly recordTypes = computed(() => this.catalog.value().recordTypes);

  readonly standaloneRecordTypes = computed(() => {
    return this.recordTypes().filter(
      (recordType) => recordType.isStandaloneExportable
    );
  });

  readonly availableFields = computed<TransferField[]>(() => {
    const recordType = this.recordTypes().find(
      (candidate) => candidate.key === this.recordType()
    );

    return recordType?.fields ?? [];
  });

  readonly stepBlocker = computed<string | null>(() => {
    const choosingFields = this.activeIndex() === 1 && !this.isArchive();
    const noFields = this.fields().length === 0;

    if (choosingFields && noFields) {
      return $localize`:Explains why an export cannot leave the field step:Choose at least one field to export.`;
    }

    return null;
  });

  readonly canGoNext = computed(() => {
    const isLastStep = this.activeIndex() === ExportLastStepIndex;

    return !isLastStep && this.stepBlocker() === null;
  });

  constructor() {
    effect(() => {
      const fields = this.availableFields();
      const alreadyChosen = this.fields().length > 0;

      if (this.isArchive() || fields.length === 0 || alreadyChosen) {
        return;
      }

      this.selectDefaultFields();
    });
  }

  back() {
    this.activeIndex.update((index) => Math.max(index - 1, 0));
  }

  next() {
    if (!this.canGoNext()) return;

    this.activeIndex.update((index) =>
      Math.min(index + 1, ExportLastStepIndex)
    );
  }

  isFieldSelected(key: string): boolean {
    return this.fields().includes(key);
  }

  clearFilterList(key: ExportFilterListKey) {
    this.filter.update((filter) => ({ ...filter, [key]: [] }));
  }

  clearFilters() {
    this.filter.set(emptyExportFilter());
  }

  toggleField(key: string, selected: boolean) {
    this.fields.update((fields) => {
      const without = fields.filter((field) => field !== key);

      return selected ? [...without, key] : without;
    });
  }

  setFields(keys: string[]) {
    this.fields.set([...keys]);
  }

  selectAllFields() {
    this.setFields(this.availableFields().map((field) => field.key));
  }

  selectDefaultFields() {
    const defaults = this.availableFields().filter(
      (field) => field.isExportedByDefault
    );

    this.setFields(defaults.map((field) => field.key));
  }

  selectRecordType(key: string) {
    this.recordType.set(key);
    this.format.set(ExportFormat.csv);
    this.patchOptions({
      includeMembers: false,
      includeFiles: false,
      includeHistory: false,
    });
    this.setFields([]);
  }

  selectArchive() {
    if (!this.canExportArchive()) return;

    this.recordType.set('workspace');
    this.format.set(ExportFormat.archive);
    this.setFields([]);
  }

  toggleFilterValue(
    key: ExportFilterListKey,
    value: string,
    selected: boolean
  ) {
    this.filter.update((filter) => {
      const without = filter[key].filter((item) => item !== value);

      return { ...filter, [key]: selected ? [...without, value] : without };
    });
  }

  patchFilter(patch: Partial<ExportFilterModel>) {
    this.filter.update((filter) => ({ ...filter, ...patch }));
  }

  patchOptions(patch: Partial<ExportOptionsModel>) {
    this.options.update((options) => ({ ...options, ...patch }));
  }

  load(definition: ExportDefinitionModel) {
    const isForbiddenArchive =
      definition.format === ExportFormat.archive && !this.canExportArchive();

    if (isForbiddenArchive) return;

    this.recordType.set(definition.recordType);
    this.format.set(definition.format);
    this.fields.set([...definition.fields]);
    this.filter.set({ ...emptyExportFilter(), ...(definition.filter ?? {}) });
    this.options.set({ ...defaultExportOptions(), ...definition.options });
  }

  loadDefinition(saved: ExportDefinitionViewModel) {
    if (!saved.definition) return;

    this.load(saved.definition);
  }
}
