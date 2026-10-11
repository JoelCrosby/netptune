import { HttpErrorResponse } from '@angular/common/http';
import { signal, WritableSignal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ClientResponse } from '@core/models/client-response';
import { PageDetail, PageKind, pageBodyMaxBytes } from '@core/models/pages';
import { PagesService } from '@core/services/pages.service';
import { getErrorMessage } from '@core/util/error-message';
import { SaveState } from '@static/components/save-status/save-status.component';

export interface PageDraftHooks {
  renamed(id: number, title: string): void;
  metaSaved(): void;
  failed(message: string): void;
}

export type PageMeta = Pick<PageDetail, 'status' | 'tags'>;

const conflictStatus = 409;
const utf8 = new TextEncoder();

// One open page's unsaved title and body. Saves run one at a time, each carrying the
// revision the last one returned, so a stale write comes back as a conflict.
export class PageDraft {
  readonly id: number;
  readonly kind: PageKind;

  readonly title: WritableSignal<string>;
  readonly body: WritableSignal<string>;
  // The page as the server last confirmed it, for who-edited-when.
  readonly latest: WritableSignal<PageDetail>;
  // A freshly loaded page is, by definition, saved.
  readonly state = signal<SaveState>('saved');
  readonly conflict = signal<PageDetail | null>(null);
  readonly errorMessage = signal<string | null>(null);

  private revision: number;
  private savedTitle: string;
  private inFlight = false;
  private queued = false;
  private metaQueue: Promise<void> = Promise.resolve();
  private pendingMeta = 0;

  constructor(
    page: PageDetail,
    private readonly pages: PagesService,
    private readonly hooks: PageDraftHooks
  ) {
    this.id = page.id;
    this.kind = page.kind;
    this.revision = page.revision;
    this.savedTitle = page.title;
    // Built inside a computed, which forbids writes, so the signals start with their values.
    this.title = signal(page.title);
    this.body = signal(page.body ?? '');
    this.latest = signal(page);
  }

  commitTitle(title: string) {
    this.title.set(title);

    const unchanged = title.trim() === this.savedTitle;

    if (unchanged) return;

    this.save();
  }

  commitBody(body: string) {
    this.body.set(body);
    this.save();
  }

  // Shows the change at once and sends it after any still in flight, so a second quick toggle builds on the
  // first instead of racing it. The text being typed is left alone.
  updateMeta(meta: Partial<PageMeta>) {
    const before = this.currentMeta();

    this.applyMeta(meta);
    this.pendingMeta++;

    const request = {
      status: meta.status,
      tagIds: meta.tags?.map((tag) => tag.id),
    };

    this.metaQueue = this.metaQueue.then(async () => {
      try {
        const response = await firstValueFrom(
          this.pages.update(this.id, request)
        );

        this.onMetaSaved(response);
      } catch (error: unknown) {
        this.onMetaFailed(before, error);
      }
    });
  }

  // The server replaced the content (a restored version); local edits give way to it.
  replaceWith(page: PageDetail) {
    this.revision = page.revision;
    this.savedTitle = page.title;
    this.latest.set(page);
    this.title.set(page.title);
    this.body.set(page.body ?? '');
    this.conflict.set(null);
    this.queued = false;
    this.state.set('saved');
  }

  private onMetaSaved(response: ClientResponse<PageDetail>) {
    this.pendingMeta--;

    const isLastInFlight = this.pendingMeta === 0;

    if (isLastInFlight && response.payload) {
      this.applyMeta(response.payload);
    }

    this.hooks.metaSaved();
  }

  private onMetaFailed(before: PageMeta, error: unknown) {
    this.pendingMeta--;
    this.applyMeta(before);
    this.hooks.failed(getErrorMessage(error, updateFailedMessage));
  }

  private applyMeta(meta: Partial<PageMeta>) {
    this.latest.update((current) => ({ ...current, ...meta }));
  }

  private currentMeta(): PageMeta {
    const { status, tags } = this.latest();

    return { status, tags };
  }

  // Takes the other writer's version and drops ours.
  acceptTheirs() {
    const theirs = this.conflict();

    if (!theirs) return;

    this.revision = theirs.revision;
    this.savedTitle = theirs.title;
    this.latest.set(theirs);
    this.title.set(theirs.title);
    this.body.set(theirs.body ?? '');
    this.conflict.set(null);
    this.state.set('saved');
  }

  // Writes ours on top of the other writer's version.
  keepMine() {
    const theirs = this.conflict();

    if (!theirs) return;

    this.revision = theirs.revision;
    this.conflict.set(null);
    this.save();
  }

  private save() {
    if (this.conflict()) return;

    if (this.inFlight) {
      this.queued = true;

      return;
    }

    const body = this.kind === PageKind.folder ? null : this.body();
    const tooLarge =
      body !== null && utf8.encode(body).length > pageBodyMaxBytes;

    if (tooLarge) {
      this.state.set('error');
      this.errorMessage.set(tooLargeMessage);

      return;
    }

    const title = this.title();

    this.inFlight = true;
    this.state.set('saving');
    this.errorMessage.set(null);

    this.pages
      .save(this.id, { title, body, revision: this.revision })
      .subscribe({
        next: (response) => this.onSaved(response),
        error: (error: unknown) => this.onFailed(error),
      });
  }

  private onSaved(response: ClientResponse<PageDetail>) {
    const saved = response.payload;

    this.inFlight = false;

    if (!saved) {
      this.state.set('error');

      return;
    }

    const renamed = saved.title !== this.savedTitle;

    this.revision = saved.revision;
    this.savedTitle = saved.title;
    this.latest.set(saved);
    this.state.set('saved');

    if (renamed) {
      this.hooks.renamed(this.id, saved.title);
    }

    this.runQueued();
  }

  private onFailed(error: unknown) {
    this.inFlight = false;
    this.queued = false;

    const isConflict =
      error instanceof HttpErrorResponse && error.status === conflictStatus;

    if (isConflict) {
      const response = error.error as ClientResponse<PageDetail> | null;

      this.conflict.set(response?.payload ?? null);
      this.state.set('conflict');

      return;
    }

    this.state.set('error');
    this.errorMessage.set(getErrorMessage(error, saveFailedMessage));
  }

  private runQueued() {
    if (!this.queued) return;

    this.queued = false;
    this.save();
  }
}

const tooLargeMessage = $localize`:Error shown when a page's text is over the size limit:Pages can be at most 1 MB of text.`;
const updateFailedMessage = $localize`:Error shown when changing a page's status or tags fails:The page couldn't be updated.`;
const saveFailedMessage = $localize`:Error shown when saving a page fails:The page couldn't be saved.`;
