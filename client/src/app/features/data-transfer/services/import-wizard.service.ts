import { HttpClient } from '@angular/common/http';
import {
  computed,
  DestroyRef,
  effect,
  inject,
  Injectable,
  signal,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ClientResponse } from '@core/models/client-response';
import { TransferField } from '@core/models/view-models/export-definition';
import {
  ImportMappingSuggestion,
  ImportPreviewResult,
  ImportSessionState,
  ImportSessionViewModel,
  ImportSourceProfile,
  ImportStage,
  ImproveImportMappingResult,
} from '@core/models/view-models/import-session';
import { workspaceBoardsResource } from '@core/resources/board.resource';
import { transferCatalogResource } from '@core/resources/transfer.resource';
import {
  selectCurrentWorkspace,
  selectCurrentWorkspaceIdentifier,
} from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';

const VendorNames: Record<number, string> = {
  1: 'Jira',
  2: 'Trello',
  3: 'Asana',
  4: 'Netptune',
};

export const ImportLastStepIndex = 4;

const PollIntervalMs = 2000;
const StallAfterMs = 60_000;

export interface DiagnosticGroup {
  code: string;
  message: string;
  count: number;
}

@Injectable()
export class ImportWizardService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly store = inject(Store);
  private readonly workspace = this.store.selectSignal(selectCurrentWorkspace);
  private readonly workspaceId = this.store.selectSignal(
    selectCurrentWorkspaceIdentifier
  );

  private readonly catalog = transferCatalogResource();
  private readonly boardsResource = workspaceBoardsResource();

  readonly activeIndex = signal(0);
  readonly file = signal<File | null>(null);
  readonly session = signal<ImportSessionViewModel | null>(null);
  readonly profile = signal<ImportSourceProfile | null>(null);
  readonly preview = signal<ImportPreviewResult | null>(null);
  readonly bindings = signal<Record<number, string>>({});
  readonly skipFailingRows = signal(false);
  readonly error = signal<string | null>(null);
  readonly mappingError = signal<string | null>(null);
  readonly isBusy = signal(false);
  readonly boardIdentifier = signal<string | null>(
    this.route.snapshot.queryParamMap.get('board')
  );
  readonly hasHeaderRow = signal(true);
  readonly vendor = signal(0);
  readonly assistantNote = signal<string | null>(null);
  readonly delimiterOverride = signal<string | null>(null);
  readonly selectedSheet = signal<string | null>(null);

  private pollHandle: ReturnType<typeof setInterval> | null = null;
  private lastProgressAt = 0;

  readonly boards = computed(() => {
    return this.boardsResource.value().flatMap((group) => group.boards);
  });

  readonly fileName = computed(() => {
    return this.file()?.name ?? this.session()?.originalName ?? null;
  });

  readonly vendorName = computed(() => VendorNames[this.vendor()] ?? null);

  readonly assistantAvailable = computed(() => {
    return this.workspace()?.assistantEnabled !== false;
  });

  readonly allowsDataSampling = computed(() => {
    return this.workspace()?.allowAssistantDataSampling !== false;
  });

  readonly importableFields = computed<TransferField[]>(() => {
    const task = this.catalog
      .value()
      .recordTypes.find((recordType) => recordType.key === 'task');

    return task?.fields ?? [];
  });

  readonly isRunning = computed(() => {
    return this.session()?.stage === ImportStage.committing;
  });

  readonly stalled = signal(false);

  readonly stepBlocker = computed<string | null>(() => {
    const stage = this.session()?.stage;

    switch (this.activeIndex()) {
      case 0:
        return this.session()
          ? null
          : $localize`:Explains why an import cannot leave the upload step:Choose a file and press Upload before you continue.`;
      case 1:
        return this.profile()
          ? null
          : $localize`:Explains why an import cannot leave the source step:Upload a file so its columns can be read.`;
      case 2:
        return stage !== undefined && stage >= ImportStage.mapped
          ? null
          : $localize`:Explains why an import cannot leave the mapping step:Press Save mapping before you continue.`;
      case 3:
        return stage !== undefined && stage >= ImportStage.committing
          ? null
          : $localize`:Explains why an import cannot leave the preview step:Press Import to start the import before you continue.`;
      default:
        return null;
    }
  });

  readonly canGoNext = computed(() => {
    const isLastStep = this.activeIndex() === ImportLastStepIndex;

    return !isLastStep && this.stepBlocker() === null;
  });

  readonly canCommit = computed(() => {
    const result = this.preview();

    if (!result) return false;

    return result.willError === 0 || this.skipFailingRows();
  });

  readonly groupedDiagnostics = computed<DiagnosticGroup[]>(() => {
    const diagnostics = this.preview()?.diagnostics ?? [];
    const groups = new Map<string, DiagnosticGroup>();

    for (const diagnostic of diagnostics) {
      const existing = groups.get(diagnostic.code);

      if (existing) {
        existing.count++;
        continue;
      }

      groups.set(diagnostic.code, {
        code: diagnostic.code,
        message: diagnostic.message,
        count: 1,
      });
    }

    return [...groups.values()];
  });

  constructor() {
    effect(() => {
      if (this.isRunning()) {
        this.startPolling();

        return;
      }

      this.stopPolling();
    });

    inject(DestroyRef).onDestroy(() => this.stopPolling());

    const sessionId = this.route.snapshot.paramMap.get('sessionId');

    if (sessionId) {
      this.resume(sessionId);
    }
  }

  resume(sessionId: string) {
    this.isBusy.set(true);
    this.error.set(null);

    this.http
      .get<ClientResponse<ImportSessionState>>(
        `api/import/sessions/${sessionId}/state`
      )
      .subscribe({
        next: (response) => {
          this.isBusy.set(false);

          const state = response.payload;

          if (!state) {
            this.error.set(
              $localize`:Shown when a started import could not be reopened:This import could not be reopened.`
            );

            return;
          }

          this.restore(state);
        },
        error: () => {
          this.isBusy.set(false);
          this.error.set(
            $localize`:Shown when a started import could not be reopened:This import could not be reopened.`
          );
        },
      });
  }

  private restore(state: ImportSessionState) {
    const session = state.session;
    const profile = state.sourceProfile ?? null;

    this.session.set(session);
    this.profile.set(profile);
    this.preview.set(state.previewResult ?? null);
    this.vendor.set(session.vendorProfile);
    this.boardIdentifier.set(session.targetBoardIdentifier ?? null);

    if (profile) {
      this.hasHeaderRow.set(profile.hasHeaderRow);
      this.delimiterOverride.set(profile.delimiter ?? null);
      this.selectedSheet.set(profile.selectedSheet ?? null);
    }

    if (state.mapping) {
      this.bindings.set(this.toBindings(state.mapping.bindings));
    }

    this.openStageStep(state);
  }

  private openStageStep(state: ImportSessionState) {
    switch (state.session.stage) {
      case ImportStage.uploaded:
        this.inspect();

        return;
      case ImportStage.inspected:
        this.activeIndex.set(2);
        this.loadSuggestion();

        return;
      case ImportStage.mapped:
        this.activeIndex.set(3);
        this.loadPreview();

        return;
      case ImportStage.previewed:
        this.activeIndex.set(3);

        if (!state.previewResult) {
          this.loadPreview();
        }

        return;
      default:
        this.activeIndex.set(4);
    }
  }

  newEntityLabel(): string {
    const entities = this.preview()?.newEntities ?? [];

    return entities.map((entity) => entity.name).join(', ');
  }

  bindingFor(columnIndex: number): string | undefined {
    return this.bindings()[columnIndex];
  }

  setFile(files: File[]) {
    this.file.set(files[0] ?? null);
  }

  setDelimiter(value: string) {
    this.delimiterOverride.set(value || null);
  }

  bindColumn(columnIndex: number, fieldKey: string) {
    this.bindings.update((bindings) => {
      const entries = Object.entries(bindings).filter(
        ([index]) => Number(index) !== columnIndex
      );

      if (fieldKey) {
        entries.push([String(columnIndex), fieldKey]);
      }

      return Object.fromEntries(entries);
    });
  }

  back() {
    this.activeIndex.update((index) => Math.max(index - 1, 0));
  }

  next() {
    if (!this.canGoNext()) return;

    this.activeIndex.update((index) =>
      Math.min(index + 1, ImportLastStepIndex)
    );
  }

  upload() {
    const file = this.file();
    const board = this.boardIdentifier() ?? this.boards()[0]?.identifier;

    if (!file || !board) {
      this.error.set(
        $localize`:Shown when an import has no file or destination:Choose a file and a destination board.`
      );

      return;
    }

    const form = new FormData();

    form.append('file', file, file.name);
    this.isBusy.set(true);
    this.error.set(null);

    this.http
      .post<ClientResponse<ImportSessionViewModel>>(
        `api/import/sessions?boardIdentifier=${encodeURIComponent(board)}`,
        form
      )
      .subscribe({
        next: (response) => {
          this.isBusy.set(false);
          this.session.set(response.payload ?? null);
          this.inspect();
        },
        error: () => {
          this.isBusy.set(false);
          this.error.set(
            $localize`:Shown when an import file could not be uploaded:The file could not be uploaded.`
          );
        },
      });
  }

  inspect() {
    const current = this.session();

    if (!current) return;

    this.http
      .post<ClientResponse<ImportSourceProfile>>(
        `api/import/sessions/${current.publicId}/inspect`,
        {
          delimiter: this.delimiterOverride(),
          hasHeaderRow: this.hasHeaderRow(),
          selectedSheet: this.selectedSheet(),
        }
      )
      .subscribe({
        next: (response) => {
          this.profile.set(response.payload ?? null);
          this.activeIndex.set(1);
          this.loadSuggestion();
        },
        error: () =>
          this.error.set(
            $localize`:Shown when an import file could not be read:The file could not be read.`
          ),
      });
  }

  improveWithAssistant() {
    const current = this.session();

    if (!current) return;

    this.isBusy.set(true);
    this.assistantNote.set(null);

    this.http
      .post<ClientResponse<ImproveImportMappingResult>>(
        `api/import/sessions/${current.publicId}/suggest/assistant`,
        {}
      )
      .subscribe({
        next: (response) => {
          this.isBusy.set(false);

          const result = response.payload;

          if (!result) return;

          this.bindings.set(this.toBindings(result.mapping.bindings));

          if (result.discardedBindings > 0) {
            this.assistantNote.set(
              $localize`:Reports how many assistant suggestions were rejected:The assistant proposed ${result.discardedBindings}:count: bindings that were discarded.`
            );
          }
        },
        error: () => {
          this.isBusy.set(false);
          this.assistantNote.set(
            $localize`:Shown when the assistant could not improve a mapping:The assistant could not improve this mapping.`
          );
        },
      });
  }

  loadSuggestion() {
    const current = this.session();

    if (!current) return;

    this.http
      .post<ClientResponse<ImportMappingSuggestion>>(
        `api/import/sessions/${current.publicId}/suggest`,
        {}
      )
      .subscribe((response) => {
        const suggestion = response.payload;

        if (!suggestion) return;

        this.vendor.set(suggestion.vendor);
        this.bindings.set(this.toBindings(suggestion.mapping.bindings));
      });
  }

  saveMapping() {
    const current = this.session();

    if (!current) return;

    const mapping = {
      recordType: 'task',
      bindings: Object.entries(this.bindings()).map(([index, fieldKey]) => ({
        fieldKey,
        columnIndex: Number(index),
      })),
    };

    this.isBusy.set(true);
    this.mappingError.set(null);

    this.http
      .put<ClientResponse<ImportSessionViewModel>>(
        `api/import/sessions/${current.publicId}/mapping`,
        mapping
      )
      .subscribe({
        next: (response) => {
          this.isBusy.set(false);
          this.session.set(response.payload ?? current);
          this.activeIndex.set(3);
          this.loadPreview();
        },
        error: (response) => {
          this.isBusy.set(false);
          this.mappingError.set(
            response?.error?.message ??
              $localize`:Shown when an import mapping was rejected:This mapping is not complete yet.`
          );
        },
      });
  }

  loadPreview() {
    const current = this.session();

    if (!current) return;

    this.isBusy.set(true);

    this.http
      .post<ClientResponse<ImportPreviewResult>>(
        `api/import/sessions/${current.publicId}/preview`,
        {}
      )
      .subscribe({
        next: (response) => {
          this.isBusy.set(false);
          this.preview.set(response.payload ?? null);
        },
        error: () => {
          this.isBusy.set(false);
          this.error.set(
            $localize`:Shown when an import preview could not be built:This import could not be previewed.`
          );
        },
      });
  }

  commit() {
    const current = this.session();

    if (!current) return;

    this.isBusy.set(true);

    this.http
      .post<ClientResponse<ImportSessionViewModel>>(
        `api/import/sessions/${current.publicId}/commit?skipFailingRows=${this.skipFailingRows()}`,
        {}
      )
      .subscribe({
        next: (response) => {
          this.isBusy.set(false);
          this.session.set(response.payload ?? current);
          this.activeIndex.set(4);
        },
        error: () => {
          this.isBusy.set(false);
          this.error.set(
            $localize`:Shown when an import could not be started:The import could not be started.`
          );
        },
      });
  }

  refresh() {
    const current = this.session();

    if (!current) return;

    this.http
      .get<ImportSessionViewModel>(`api/import/sessions/${current.publicId}`)
      .subscribe((response) => this.onSessionPolled(response));
  }

  private onSessionPolled(session: ImportSessionViewModel) {
    const previous = this.session();
    const moved =
      previous?.progressPercent !== session.progressPercent ||
      previous?.stage !== session.stage;

    if (moved) {
      this.lastProgressAt = Date.now();
      this.stalled.set(false);
    }

    this.session.set(session);
  }

  private startPolling() {
    if (this.pollHandle !== null) return;

    this.lastProgressAt = Date.now();
    this.stalled.set(false);

    this.pollHandle = setInterval(() => {
      const waitedTooLong = Date.now() - this.lastProgressAt > StallAfterMs;

      if (waitedTooLong) {
        this.stalled.set(true);
      }

      this.refresh();
    }, PollIntervalMs);
  }

  private stopPolling() {
    if (this.pollHandle === null) return;

    clearInterval(this.pollHandle);
    this.pollHandle = null;
  }

  undo() {
    const current = this.session();

    if (!current) return;

    this.http
      .post(`api/import/sessions/${current.publicId}/undo`, {})
      .subscribe({
        next: () =>
          this.router.navigate([
            '/',
            this.workspaceId(),
            'settings',
            'workspace',
            'data',
          ]),
        error: () =>
          this.error.set(
            $localize`:Shown when an import could not be undone:This import could not be undone.`
          ),
      });
  }

  private toBindings(
    bindings: { fieldKey: string; columnIndex?: number | null }[]
  ): Record<number, string> {
    const bound = bindings.filter((binding) => binding.columnIndex != null);

    return Object.fromEntries(
      bound.map((binding) => [String(binding.columnIndex), binding.fieldKey])
    );
  }
}
