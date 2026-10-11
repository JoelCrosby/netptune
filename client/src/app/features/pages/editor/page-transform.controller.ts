import { HttpErrorResponse } from '@angular/common/http';
import {
  computed,
  DestroyRef,
  inject,
  Injectable,
  LOCALE_ID,
  signal,
} from '@angular/core';
import { PageTransformAction } from '@core/models/pages';
import { PagesService } from '@core/services/pages.service';
import { InlineSelectOption } from '@static/components/inline-select/inline-select.component';
import { EditorAnchor } from '@static/components/editor/editor-selection';
import {
  ResultCardAction,
  ResultCardStatus,
} from '@static/components/result-card/result-card.component';
import type { Editor } from '@tiptap/core';
import { firstValueFrom } from 'rxjs';
import { TrackedRange, trackRange } from './editor-range-tracker';

export type PageTransformPlacement = 'replace' | 'insert';

export interface PageTransformStart {
  editor: Editor;
  pageId: number;
  action: PageTransformAction;
  text: string;
  range: TrackedRange;
  anchor: EditorAnchor;
  placements: readonly PageTransformPlacement[];
}

export interface PageTransformAccepted {
  placement: PageTransformPlacement;
  range: TrackedRange;
  text: string;
}

const LANGUAGE_CODES = [
  'en',
  'fr',
  'de',
  'es',
  'it',
  'pt',
  'nl',
  'pl',
  'ja',
  'zh',
  'ko',
];

const CARD_HEIGHT = 360;
const CARD_WIDTH = 420;
const VIEWPORT_GUTTER = 16;

const HEADINGS: Record<PageTransformAction, string> = {
  summarise: $localize`:Heading of the card showing an assistant summary of page text:Summary`,
  tighten: $localize`:Heading of the card showing page text rewritten shorter by the assistant:Tightened`,
  translate: $localize`:Heading of the card showing page text translated by the assistant:Translation`,
  continue: $localize`:Heading of the card showing text the assistant wrote to continue a page:Continued`,
};

const PLACEMENT_LABELS: Record<PageTransformPlacement, string> = {
  replace: $localize`:Button that swaps the selected page text for the assistant's version:Replace selection`,
  insert: $localize`:Button that adds the assistant's text below the selection or cursor:Insert below`,
};

const LOST = $localize`:Shown when the page text an assistant result was for has been replaced:The text this was for has changed. Select it again and retry.`;

const FAILED = $localize`:Shown when the assistant could not rewrite page text:The assistant could not do that just now. Try again.`;

@Injectable()
export class PageTransformController {
  private readonly pages = inject(PagesService);
  private readonly locale = inject(LOCALE_ID);

  readonly open = signal(false);
  readonly status = signal<ResultCardStatus>('loading');
  readonly action = signal<PageTransformAction>('summarise');
  readonly text = signal('');
  readonly error = signal('');
  readonly anchor = signal<EditorAnchor>({ left: 0, top: 0 });
  readonly language = signal(this.localeLanguage());

  readonly languageOptions: InlineSelectOption[] = this.buildLanguageOptions();

  readonly heading = computed(() => HEADINGS[this.action()]);

  readonly actions = computed<ResultCardAction[]>(() => {
    return this.placements().map((placement, index) => {
      return {
        id: placement,
        label: PLACEMENT_LABELS[placement],
        primary: index === 0,
      };
    });
  });

  readonly position = computed(() => {
    const anchor = this.anchor();
    const width = Math.min(CARD_WIDTH, window.innerWidth - 2 * VIEWPORT_GUTTER);
    const rightmostLeft = window.innerWidth - width - VIEWPORT_GUTTER;
    const lowestTop = window.innerHeight - CARD_HEIGHT - VIEWPORT_GUTTER;

    return {
      left: Math.max(VIEWPORT_GUTTER, Math.min(anchor.left, rightmostLeft)),
      top: Math.max(VIEWPORT_GUTTER, Math.min(anchor.top, lowestTop)),
    };
  });

  private readonly placements = signal<readonly PageTransformPlacement[]>([]);
  private request: Omit<PageTransformStart, 'range' | 'editor'> | null = null;
  private tracker: ReturnType<typeof trackRange> | null = null;
  private generation = 0;

  constructor() {
    inject(DestroyRef).onDestroy(() => this.close());
  }

  start(start: PageTransformStart) {
    this.close();

    this.request = start;
    this.tracker = trackRange(start.editor, start.range);
    this.action.set(start.action);
    this.anchor.set(start.anchor);
    this.placements.set(start.placements);
    this.open.set(true);

    void this.run();
  }

  reanchor(anchorFor: (range: TrackedRange) => EditorAnchor) {
    const range = this.tracker?.current();

    if (!this.open() || !range) return;

    this.anchor.set(anchorFor(range));
  }

  setLanguage(language: string) {
    this.language.set(language);

    void this.run();
  }

  accept(placement: string): PageTransformAccepted | null {
    const range = this.tracker?.current();
    const isPlacement = this.placements().includes(
      placement as PageTransformPlacement
    );
    const isReady = this.status() === 'ready';

    if (!isPlacement || !isReady) return null;

    if (!range) {
      this.fail(LOST);

      return null;
    }

    const accepted = {
      placement: placement as PageTransformPlacement,
      range,
      text: this.text(),
    };

    this.close();

    return accepted;
  }

  close() {
    this.generation++;
    this.tracker?.stop();
    this.tracker = null;
    this.request = null;
    this.open.set(false);
  }

  private async run() {
    const request = this.request;

    if (!request) return;

    const generation = ++this.generation;
    const isTranslation = request.action === 'translate';

    this.status.set('loading');

    try {
      const response = await firstValueFrom(
        this.pages.transform({
          pageId: request.pageId,
          action: request.action,
          text: request.text,
          language: isTranslation ? this.language() : undefined,
        })
      );

      if (generation !== this.generation) return;

      if (!response.isSuccess || !response.payload) {
        this.fail(response.message ?? FAILED);
        return;
      }

      this.text.set(response.payload.text);
      this.status.set('ready');
    } catch (error: unknown) {
      if (generation !== this.generation) return;

      this.fail(readServerMessage(error) ?? FAILED);
    }
  }

  private fail(message: string) {
    this.error.set(message);
    this.status.set('error');
  }

  private localeLanguage(): string {
    const english = new Intl.DisplayNames(['en'], { type: 'language' });
    const code = this.locale.split('-')[0];

    return english.of(code) ?? 'English';
  }

  // The value sent is the English name, which the model reads; the label is in the reader's language.
  private buildLanguageOptions(): InlineSelectOption[] {
    const english = new Intl.DisplayNames(['en'], { type: 'language' });
    const local = new Intl.DisplayNames([this.locale], { type: 'language' });

    return LANGUAGE_CODES.map((code) => ({
      value: english.of(code) ?? code,
      label: local.of(code) ?? code,
    }));
  }
}

function readServerMessage(error: unknown): string | null {
  const isHttpError = error instanceof HttpErrorResponse;

  if (!isHttpError) return null;

  const body: unknown = error.error;
  const message = (body as { message?: unknown } | null)?.message;

  return typeof message === 'string' ? message : null;
}
