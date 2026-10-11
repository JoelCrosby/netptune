import { inject, Service } from '@angular/core';
import {
  ArchivedPage,
  CreatePageRequest,
  MovePageRequest,
  PageDetail,
  PageKind,
  ImportPagesResult,
} from '@core/models/pages';
import { ConfirmationService } from '@core/services/confirmation.service';
import { PagesService } from '@core/services/pages.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { getErrorMessage } from '@core/util/error-message';
import {
  requireSuccess,
  unwrapClientResponse,
} from '@core/util/rxjs-operators';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import {
  catchError,
  defer,
  EMPTY,
  map,
  Observable,
  switchMap,
  tap,
} from 'rxjs';

@Service()
export class PageCommandsService {
  private readonly pages = inject(PagesService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);
  private readonly snackbar = inject(SnackbarService);

  create(request: CreatePageRequest): Observable<PageDetail> {
    return defer(() => {
      return this.pages.create(request).pipe(
        unwrapClientResponse(),
        tap(() => this.refresh()),
        catchError((error: unknown) => this.report(error, createFailed))
      );
    });
  }

  move(id: number, request: MovePageRequest) {
    this.pages
      .move(id, request)
      .pipe(
        requireSuccess(),
        catchError((error: unknown) => {
          this.refresh();

          return this.report(error, moveFailed);
        })
      )
      .subscribe(() => this.refresh());
  }

  restoreVersion(id: number, versionId: number): Observable<PageDetail> {
    return defer(() => {
      return this.pages.restoreVersion(id, versionId).pipe(
        unwrapClientResponse(),
        tap(() => {
          this.snackbar.success(versionRestored);
          this.refresh();
        }),
        catchError((error: unknown) => this.report(error, restoreVersionFailed))
      );
    });
  }

  archive(page: Pick<PageDetail, 'id' | 'kind'>): Observable<void> {
    const options =
      page.kind === PageKind.folder
        ? ARCHIVE_FOLDER_CONFIRMATION
        : ARCHIVE_PAGE_CONFIRMATION;

    return defer(() => {
      return this.confirmation.open(options).pipe(
        switchMap((confirmed) => {
          if (!confirmed) return EMPTY;

          return this.pages.archive(page.id).pipe(requireSuccess());
        }),
        tap(() => {
          this.snackbar.success(archived);
          this.refresh();
        }),
        map(() => undefined),
        catchError((error: unknown) => this.report(error, archiveFailed))
      );
    });
  }

  deletePermanently(page: Pick<ArchivedPage, 'id' | 'kind'>) {
    const options =
      page.kind === PageKind.folder
        ? DELETE_FOLDER_CONFIRMATION
        : DELETE_PAGE_CONFIRMATION;

    this.confirmation
      .open(options)
      .pipe(
        switchMap((confirmed) => {
          if (!confirmed) return EMPTY;

          return this.pages.deletePermanently(page.id).pipe(requireSuccess());
        }),
        catchError((error: unknown) => this.report(error, deleteFailed))
      )
      .subscribe(() => {
        this.snackbar.success(deletedPermanently);
        this.refresh();
      });
  }

  restore(id: number) {
    this.pages
      .restore(id)
      .pipe(
        requireSuccess(),
        catchError((error: unknown) => this.report(error, restoreFailed))
      )
      .subscribe(() => {
        this.snackbar.success(restored);
        this.refresh();
      });
  }

  importPages(
    files: readonly File[],
    projectId: number | null
  ): Observable<ImportPagesResult> {
    return defer(() => {
      return this.pages.import(files, projectId).pipe(
        unwrapClientResponse(),
        tap((result) => {
          this.refresh();
          this.snackbar.success(importedMessage(result));
        }),
        catchError((error: unknown) => this.report(error, importFailed))
      );
    });
  }

  markReviewed(id: number) {
    this.pages
      .markReviewed(id)
      .pipe(
        requireSuccess(),
        catchError((error: unknown) => this.report(error, reviewFailed))
      )
      .subscribe(() => this.refresh());
  }

  setFavourite(id: number, isFavourite: boolean) {
    const request = isFavourite
      ? this.pages.favourite(id)
      : this.pages.unfavourite(id);

    request
      .pipe(
        requireSuccess(),
        catchError((error: unknown) => this.report(error, favouriteFailed))
      )
      .subscribe(() => this.refresh());
  }

  recordVisit(id: number) {
    this.pages
      .recordVisit(id)
      .pipe(catchError(() => EMPTY))
      .subscribe();
  }

  private refresh() {
    this.workspaceRefresh.refresh(['pages']);
  }

  private report(error: unknown, fallback: string) {
    this.snackbar.error(getErrorMessage(error, fallback));

    return EMPTY;
  }
}

const createFailed = $localize`:Error shown when creating a page fails:The page couldn't be created.`;
const moveFailed = $localize`:Error shown when moving a page in the tree fails:The page couldn't be moved.`;
const archiveFailed = $localize`:Error shown when archiving a page fails:The page couldn't be archived.`;
const restoreFailed = $localize`:Error shown when restoring an archived page fails:The page couldn't be restored.`;
const importFailed = $localize`:Error shown when importing markdown files as pages fails:The files couldn't be imported.`;

function importedMessage(result: ImportPagesResult): string {
  const skipped = result.skipped.length;
  const isOnePage = result.created === 1;

  if (!skipped) {
    return isOnePage
      ? $localize`:Shown after a single markdown file was imported as a page:Imported 1 page`
      : $localize`:Shown after markdown files were imported as pages:Imported ${result.created}:COUNT: pages`;
  }

  return isOnePage
    ? $localize`:Shown after an import of a single page that left some files out:Imported 1 page, skipped ${skipped}:SKIPPED: files`
    : $localize`:Shown after an import that left some files out:Imported ${result.created}:COUNT: pages, skipped ${skipped}:SKIPPED: files`;
}

const reviewFailed = $localize`:Error shown when marking a page as reviewed fails:The page couldn't be marked as reviewed.`;
const favouriteFailed = $localize`:Error shown when adding or removing a page favourite fails:Favourites couldn't be updated.`;
const restoreVersionFailed = $localize`:Error shown when restoring an earlier version of a page fails:That version couldn't be restored.`;
const versionRestored = $localize`:Confirmation shown after an earlier version of a page is restored:Version restored`;
const archived = $localize`:Confirmation shown after a page is archived:Page archived`;
const deletedPermanently = $localize`:Confirmation shown after an archived page is deleted for good:Page deleted`;
const deleteFailed = $localize`:Error shown when deleting an archived page for good fails:The page couldn't be deleted.`;
const restored = $localize`:Confirmation shown after an archived page is restored:Page restored`;

const ARCHIVE_PAGE_CONFIRMATION: ConfirmDialogOptions = {
  title: $localize`:Title of the dialog confirming a page is archived:Archive Page`,
  message: $localize`:Body of the dialog confirming a page is archived:The page and any pages inside it move to the archive. You can restore them later.`,
  acceptLabel: $localize`:Confirms archiving a page:Archive Page`,
  cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
  color: 'warn',
};

const DELETE_PAGE_CONFIRMATION: ConfirmDialogOptions = {
  title: $localize`:Title of the dialog confirming an archived page is deleted for good:Delete Page`,
  message: $localize`:Body of the dialog confirming an archived page is deleted for good:The page, its history and any archived pages inside it are deleted for good. This cannot be undone.`,
  acceptLabel: $localize`:Confirms deleting an archived page for good:Delete Page`,
  cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
  color: 'warn',
};

const DELETE_FOLDER_CONFIRMATION: ConfirmDialogOptions = {
  title: $localize`:Title of the dialog confirming an archived folder is deleted for good:Delete Folder`,
  message: $localize`:Body of the dialog confirming an archived folder is deleted for good:The folder and every archived page inside it are deleted for good, with their history. This cannot be undone.`,
  acceptLabel: $localize`:Confirms deleting an archived folder for good:Delete Folder`,
  cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
  color: 'warn',
};

const ARCHIVE_FOLDER_CONFIRMATION: ConfirmDialogOptions = {
  title: $localize`:Title of the dialog confirming a folder is archived:Archive Folder`,
  message: $localize`:Body of the dialog confirming a folder is archived:The folder and everything inside it move to the archive. You can restore them later.`,
  acceptLabel: $localize`:Confirms archiving a folder:Archive Folder`,
  cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
  color: 'warn',
};
