import {
  computed,
  DestroyRef,
  inject,
  Service,
  Signal,
  signal,
  untracked,
} from '@angular/core';
import { BoardViewModel } from '@core/models/view-models/board-view-model';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { onChange } from '@core/util/signals';

export interface CurrentPage {
  id: number;
  slug: string;
  title: string;
}

interface CurrentContext {
  task: TaskViewModel;
  page: CurrentPage;
  board: BoardViewModel;
  sprint: SprintViewModel;
}

type ContextKind = keyof CurrentContext;

interface Publisher<K extends ContextKind> {
  owner: object;
  source: Signal<CurrentContext[K] | undefined>;
  staleValue: CurrentContext[K] | undefined;
}

type Publishers = { [K in ContextKind]: Publisher<K>[] };

@Service()
export class CurrentContextService {
  private readonly publishers = signal<Publishers>({
    task: [],
    page: [],
    board: [],
    sprint: [],
  });

  readonly task = this.select('task');
  readonly page = this.select('page');
  readonly board = this.select('board');
  readonly sprint = this.select('sprint');

  constructor() {
    // The guard switches workspace before the old view is torn down, so what each
    // view holds then belongs to the old workspace until it loads something new.
    onChange(inject(CurrentWorkspaceService).id, () => this.markAllStale());
  }

  // The most recent publisher with a value wins, so closing a view that opened over
  // another hands the context back to the one underneath.
  publish<K extends ContextKind>(
    kind: K,
    source: Signal<CurrentContext[K] | undefined>
  ) {
    const owner = {};
    const publisher: Publisher<K> = { owner, source, staleValue: undefined };

    this.update(kind, (publishers) => [...publishers, publisher]);

    inject(DestroyRef).onDestroy(() => {
      this.update(kind, (publishers) => {
        return publishers.filter((current) => current.owner !== owner);
      });
    });
  }

  private select<K extends ContextKind>(kind: K) {
    return computed(() => {
      const publishers = this.publishers()[kind];

      for (const publisher of [...publishers].reverse()) {
        const value = publisher.source();
        const isFresh = value !== undefined && value !== publisher.staleValue;

        if (isFresh) return value;
      }

      return undefined;
    });
  }

  private markAllStale() {
    const kinds = Object.keys(untracked(this.publishers)) as ContextKind[];

    for (const kind of kinds) {
      this.update(kind, (publishers) => {
        return publishers.map((publisher) => {
          return { ...publisher, staleValue: untracked(publisher.source) };
        });
      });
    }
  }

  private update<K extends ContextKind>(
    kind: K,
    change: (publishers: Publisher<K>[]) => Publisher<K>[]
  ) {
    this.publishers.update((all) => {
      return { ...all, [kind]: change(all[kind]) };
    });
  }
}
