import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import {
  Component,
  computed,
  DestroyRef,
  inject,
  resource,
  signal,
} from '@angular/core';
import {
  apply,
  form,
  FormField,
  required,
  submit,
  validateAsync,
} from '@angular/forms/signals';
import { FlatButtonComponent } from '@app/static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@app/static/components/button/stroked-button.component';
import { Board } from '@core/models/board';
import { ClientResponse } from '@core/models/client-response';
import { BrandingTarget } from '@core/models/branding';
import { UpdateBoardRequest } from '@core/models/requests/update-board-request';
import { BoardsService } from '@core/services/boards.service';
import { BrandingService } from '@core/services/branding.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { brandingImageUrl } from '@core/util/branding';
import { resolveColorName } from '@core/util/colors/colors';
import { getErrorMessage } from '@core/util/error-message';
import { requiredTextSchema } from '@core/util/forms/validation.schemas';
import { unwrapClientResponse } from '@core/util/rxjs-operators';
import { LucideCheck } from '@lucide/angular';
import {
  BrandingImageKind,
  BrandingPreviewComponent,
} from '@static/components/branding-preview/branding-preview.component';
import { BusyOverlayComponent } from '@static/components/busy-overlay.component';
import { ColorSelectComponent } from '@static/components/color-select/color-select.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';
import { firstValueFrom, Observable } from 'rxjs';
import { map } from 'rxjs/operators';

// A logo or background as it will be once saved: the stored file, a newly chosen
// file waiting to upload, or nothing.
interface ImageDraft {
  fileId: string | null;
  file: File | null;
  previewUrl: string | null;
}

@Component({
  selector: 'app-edit-board',
  imports: [
    DialogTitleComponent,
    DialogActionsDirective,
    DialogCloseDirective,
    FormField,
    FormInputComponent,
    ColorSelectComponent,
    BusyOverlayComponent,
    BrandingPreviewComponent,
    FlatButtonComponent,
    StrokedButtonComponent,
  ],
  template: `
    <app-dialog-title showCloseButton>
      <span i18n="Title of the edit-board dialog">Edit Board</span>
    </app-dialog-title>

    <app-busy-overlay [busy]="saving()" spinnerDiameter="24px">
      <div class="flex flex-col gap-6">
        <app-branding-preview
          [title]="boardModel().name"
          [subtitle]="previewPath()"
          [color]="boardModel().color"
          [logoUrl]="logo().previewUrl"
          [backgroundUrl]="background().previewUrl"
          [disabled]="saving()"
          (fileSelected)="chooseImage($event.kind, $event.file)"
          (removed)="removeImage($event)" />

        <div
          class="grid grid-cols-1 gap-3.5 sm:grid-cols-[minmax(0,1.2fr)_minmax(0,1fr)]">
          <app-form-input
            [noMargin]="true"
            [formField]="boardForm.name"
            i18n-label="Label of the board name field"
            label="Name"
            i18n-placeholder="Placeholder of the board name field"
            placeholder="Board name"
            maxLength="1024" />

          <app-form-input
            [noMargin]="true"
            [formField]="boardForm.identifier"
            i18n-label="Label of the board URL identifier field"
            label="Identifier"
            maxLength="1024"
            [hint]="identifierHint()"
            [icon]="identifierIcon()"
            [loading]="boardForm.identifier().pending()" />
        </div>

        <app-color-select
          [formField]="boardForm.color"
          i18n-label="Label of the colour picker field"
          label="Colour" />
      </div>
    </app-busy-overlay>

    <div app-dialog-actions class="items-center">
      <span class="text-muted flex-1 text-xs" aria-live="polite">
        {{ changesLabel() }}
      </span>
      @if (changeCount() > 0) {
        <button
          app-flat-button
          color="ghost"
          type="button"
          [disabled]="saving()"
          (click)="reset()">
          <span i18n="Button that discards unsaved edits to the board">
            Reset
          </span>
        </button>
      }
      <button app-stroked-button app-dialog-close type="button">
        <span i18n="Dismisses a dialog without saving">Cancel</span>
      </button>
      <button
        app-flat-button
        type="button"
        [disabled]="!canSave()"
        (click)="save()">
        <span i18n="Button that saves edits to the board">Save Changes</span>
      </button>
    </div>
  `,
})
export class EditBoardComponent {
  private readonly boardsService = inject(BoardsService);
  private readonly brandingService = inject(BrandingService);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);
  private readonly snackbar = inject(SnackbarService);
  private readonly workspaceSlug = inject(CurrentWorkspaceService).slug;
  private readonly dialogRef = inject<DialogRef<EditBoardComponent>>(DialogRef);
  private readonly board = inject<Board>(DIALOG_DATA);

  private readonly initialModel = {
    name: this.board.name ?? '',
    identifier: this.board.identifier ?? '',
    color: resolveColorName(this.board.metaInfo?.color),
  };

  private readonly initialLogo = this.savedImage(
    this.board.metaInfo?.logoFileId
  );
  private readonly initialBackground = this.savedImage(
    this.board.metaInfo?.backgroundFileId
  );

  private readonly objectUrls = new Set<string>();

  protected readonly boardModel = signal({ ...this.initialModel });
  protected readonly logo = signal(this.initialLogo);
  protected readonly background = signal(this.initialBackground);
  protected readonly saving = signal(false);

  protected readonly boardForm = form(this.boardModel, (schema) => {
    apply(
      schema.name,
      requiredTextSchema({
        label: $localize`:Field name used inside validation messages, e.g. "Board name is required.":Board name`,
        maxLength: 1024,
      })
    );
    apply(
      schema.identifier,
      requiredTextSchema({
        label: $localize`:Field name used inside validation messages, e.g. "Board identifier is required.":Board identifier`,
        minLength: 4,
        maxLength: 1024,
      })
    );
    required(schema.color);
    validateAsync(schema.identifier, {
      params: ({ value }) => {
        const identifier = value();

        if (identifier === this.initialModel.identifier) return undefined;
        if (!identifier || identifier.length < 4) return undefined;

        return identifier;
      },
      factory: (params) =>
        resource({
          params: params,
          loader: ({ params }) => {
            const request = this.boardsService
              .isIdentifierUnique(params)
              .pipe(map((response) => response?.payload?.isUnique ?? false));

            return firstValueFrom(request);
          },
        }),
      onSuccess: (isUnique) => {
        if (isUnique) {
          return undefined;
        }

        return {
          kind: 'identifierTaken',
          message: $localize`:Validation error when a board identifier is already in use:Another board already uses this identifier.`,
        };
      },
      onError: () => ({
        kind: 'networkError',
        message: $localize`:Shown when the board identifier availability check fails:Could not verify identifier availability.`,
      }),
    });
  });

  protected readonly previewPath = computed(() => {
    const identifier = this.boardModel().identifier || '…';

    return `/boards/${identifier}`;
  });

  protected readonly identifierIcon = computed(() => {
    const field = this.boardForm.identifier();

    if (field.pending() || !field.valid()) return null;

    return LucideCheck;
  });

  protected readonly identifierHint = computed(() => {
    const field = this.boardForm.identifier();

    if (field.pending() || !field.valid()) return null;

    return $localize`:Hint under a valid board identifier:Used in this board's URL.`;
  });

  private readonly changedFields = computed(() => {
    const model = this.boardModel();

    return {
      name: model.name !== this.initialModel.name,
      identifier: model.identifier !== this.initialModel.identifier,
      color: model.color !== this.initialModel.color,
      logo: this.isImageChanged(this.logo(), this.initialLogo),
      background: this.isImageChanged(
        this.background(),
        this.initialBackground
      ),
    };
  });

  protected readonly changeCount = computed(() => {
    return Object.values(this.changedFields()).filter(Boolean).length;
  });

  protected readonly changesLabel = computed(() => {
    const count = this.changeCount();

    if (count === 0) return '';
    if (count === 1) {
      return $localize`:Footer note in the edit-board dialog:1 unsaved change`;
    }

    return $localize`:Footer note in the edit-board dialog. COUNT is a number greater than one:${count}:COUNT: unsaved changes`;
  });

  protected readonly canSave = computed(() => {
    if (this.saving()) return false;
    if (this.changeCount() === 0) return false;

    const identifier = this.boardForm.identifier();

    return !identifier.pending() && this.boardForm().valid();
  });

  constructor() {
    inject(DestroyRef).onDestroy(() => {
      this.objectUrls.forEach((url) => URL.revokeObjectURL(url));
    });
  }

  protected chooseImage(kind: BrandingImageKind, file: File) {
    const previewUrl = URL.createObjectURL(file);

    this.objectUrls.add(previewUrl);
    this.imageSignal(kind).set({ fileId: null, file, previewUrl });
  }

  protected removeImage(kind: BrandingImageKind) {
    this.imageSignal(kind).set({ fileId: null, file: null, previewUrl: null });
  }

  protected reset() {
    this.boardModel.set({ ...this.initialModel });
    this.logo.set(this.initialLogo);
    this.background.set(this.initialBackground);
  }

  protected save() {
    if (!this.canSave()) return;

    submit(this.boardForm, async () => {
      this.saving.set(true);

      try {
        await Promise.all(this.saveRequests());
      } catch (error: unknown) {
        this.snackbar.error(getErrorMessage(error));

        return;
      } finally {
        this.saving.set(false);
        this.workspaceRefresh.refresh(['boards', 'boardGroups']);
      }

      this.dialogRef.close();
    });
  }

  private saveRequests() {
    const changed = this.changedFields();
    const requests: Promise<unknown>[] = [];

    if (changed.name || changed.identifier || changed.color) {
      const model = this.boardModel();
      const request: UpdateBoardRequest = {
        id: this.board.id,
        name: model.name.trim(),
        identifier: model.identifier.trim(),
        meta: { color: model.color },
      };

      requests.push(this.unwrap(this.boardsService.put(request)));
    }

    if (changed.logo) {
      requests.push(
        this.saveImage(
          { kind: 'boardLogo', boardId: this.board.id },
          this.logo()
        )
      );
    }

    if (changed.background) {
      requests.push(
        this.saveImage(
          { kind: 'boardBackground', boardId: this.board.id },
          this.background()
        )
      );
    }

    return requests;
  }

  private saveImage(target: BrandingTarget, draft: ImageDraft) {
    if (draft.file) {
      return this.unwrap(this.brandingService.upload(target, draft.file));
    }

    return this.unwrap(this.brandingService.remove(target));
  }

  private unwrap(source: Observable<ClientResponse<unknown>>) {
    const request = source.pipe(unwrapClientResponse());

    return firstValueFrom(request);
  }

  private imageSignal(kind: BrandingImageKind) {
    return kind === 'logo' ? this.logo : this.background;
  }

  private savedImage(fileId: string | null | undefined): ImageDraft {
    const previewUrl = brandingImageUrl(this.workspaceSlug(), fileId);

    return { fileId: fileId ?? null, file: null, previewUrl };
  }

  private isImageChanged(draft: ImageDraft, initial: ImageDraft) {
    return draft.file !== null || draft.fileId !== initial.fileId;
  }
}
