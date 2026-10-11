import { httpResource, HttpParams } from '@angular/common/http';
import { computed, debounced, inject, Injectable, signal } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { SearchResponse, SearchResult } from '@core/models/search-result';
import { LucideFileText, LucideSquareCheck } from '@lucide/angular';
import {
  initialSuggestionMenuPosition,
  suggestionMenuPosition,
} from '@static/components/editor/editor-suggestion';
import { SuggestionItem } from '@static/components/suggestion-menu/suggestion-menu.component';
import type {
  SuggestionKeyDownProps,
  SuggestionProps,
} from '@tiptap/suggestion';
import { PageTreeStore } from '../services/page-tree.store';
import { PageReferenceAttrs } from './page-reference-syntax';

const maxTasks = 6;
const maxPages = 5;

type ReferenceSuggestionProps = SuggestionProps<unknown, PageReferenceAttrs>;

interface Candidate {
  item: SuggestionItem;
  attrs: PageReferenceAttrs;
}

export interface PickedTask {
  systemId: string;
  title: string;
}

// The @ menu: what has been typed, what matches, which row is active, and inserting the pick.
@Injectable()
export class PageReferenceController {
  private readonly store = inject(PageTreeStore);
  private readonly canSearchTasks = hasPermission(PERMISSIONS.tasks.read);

  readonly open = signal(false);
  readonly position = signal(initialSuggestionMenuPosition);
  readonly activeIndex = signal(0);
  // Tasks inserted before the next save has linked them, keyed by lower-cased system id, so their
  // chips can show a title straight away.
  readonly pickedTasks = signal<ReadonlyMap<string, PickedTask>>(new Map());

  private readonly query = signal('');
  private readonly settledQuery = debounced(this.query, 200);
  private excludedPageId: number | null = null;
  private insert: ((attrs: PageReferenceAttrs) => void) | null = null;

  private readonly taskSearch = httpResource<SearchResponse>(() => {
    const query = this.settledQuery.value().trim();
    const canSearch = this.open() && query.length > 0 && this.canSearchTasks();

    if (!canSearch) return undefined;

    const params = new HttpParams().set('q', query).append('types', 'tasks');

    return { url: 'api/search', params };
  });

  // Pages are local and match by title, so a title that starts with what was typed ranks above the
  // task search; otherwise tasks lead, as they are mentioned far more often.
  private readonly candidates = computed<Candidate[]>(() => {
    const pages = this.pageCandidates();
    const tasks = this.taskCandidates();
    const term = this.query().trim().toLocaleLowerCase();
    const pageLeads =
      term.length > 0 && pages.some((page) => startsWith(page, term));

    return pageLeads ? [...pages, ...tasks] : [...tasks, ...pages];
  });

  readonly items = computed(() => {
    return this.candidates().map((candidate) => candidate.item);
  });

  readonly handlers = {
    start: (props: ReferenceSuggestionProps) => this.onStart(props),
    update: (props: ReferenceSuggestionProps) => this.onUpdate(props),
    keyDown: (props: SuggestionKeyDownProps) => this.onKeyDown(props),
    exit: () => this.close(),
  };

  excludePage(pageId: number) {
    this.excludedPageId = pageId;
  }

  pick(index: number) {
    const candidate = this.candidates().at(index);

    if (!candidate || !this.insert) return;

    if (candidate.attrs.kind === 'task') {
      this.rememberTask(candidate);
    }

    this.insert(candidate.attrs);
  }

  private onStart(props: ReferenceSuggestionProps) {
    this.open.set(true);
    this.activeIndex.set(0);
    this.onUpdate(props);
  }

  private onUpdate(props: ReferenceSuggestionProps) {
    this.insert = props.command;
    this.query.set(props.query);
    this.activeIndex.set(0);

    const rect = props.clientRect?.();

    if (rect) {
      this.position.set(suggestionMenuPosition(rect));
    }
  }

  private onKeyDown({ event }: SuggestionKeyDownProps): boolean {
    const count = this.items().length;

    switch (event.key) {
      case 'ArrowDown':
        if (!count) return false;

        this.moveActive(1, count);

        return true;
      case 'ArrowUp':
        if (!count) return false;

        this.moveActive(-1, count);

        return true;
      case 'Enter':
      case 'Tab':
        if (!count) return false;

        this.pick(this.activeIndex());

        return true;
      case 'Escape':
        this.close();

        return true;
      default:
        return false;
    }
  }

  private moveActive(step: number, count: number) {
    this.activeIndex.update((index) => (index + step + count) % count);
  }

  private close() {
    this.open.set(false);
    this.insert = null;
  }

  private taskCandidates(): Candidate[] {
    const results = this.taskSearch.hasValue()
      ? this.taskSearch.value().results
      : [];

    return results
      .filter((result) => result.type === 'task')
      .map(toTaskCandidate)
      .filter((candidate): candidate is Candidate => candidate !== null)
      .slice(0, maxTasks);
  }

  private pageCandidates(): Candidate[] {
    const term = this.query().trim().toLocaleLowerCase();

    return this.store
      .nodes()
      .filter((node) => node.id !== this.excludedPageId)
      .filter((node) => node.title.toLocaleLowerCase().includes(term))
      .slice(0, maxPages)
      .map((node) => {
        return {
          item: {
            id: `page:${node.id}`,
            label: node.title,
            icon: LucideFileText,
            group: pagesGroup,
          },
          attrs: { kind: 'page', key: String(node.id), label: node.title },
        };
      });
  }

  private rememberTask(candidate: Candidate) {
    const picked: PickedTask = {
      systemId: candidate.attrs.key,
      title: candidate.item.label,
    };
    const lookupKey = candidate.attrs.key.toLowerCase();

    this.pickedTasks.update((current) => {
      return new Map(current).set(lookupKey, picked);
    });
  }
}

function toTaskCandidate(result: SearchResult): Candidate | null {
  const systemId = result.metadata['systemId'];

  if (typeof systemId !== 'string' || !systemId) return null;

  return {
    item: {
      id: `task:${systemId}`,
      label: result.title,
      hint: systemId,
      icon: LucideSquareCheck,
      group: tasksGroup,
    },
    attrs: { kind: 'task', key: systemId, label: null },
  };
}

function startsWith(candidate: Candidate, term: string): boolean {
  return candidate.item.label.toLocaleLowerCase().startsWith(term);
}

const tasksGroup = $localize`:Heading over tasks offered in the mention menu:Tasks`;
const pagesGroup = $localize`:Heading over pages offered in the mention menu:Pages`;
