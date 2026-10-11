import { inject, Service, signal } from '@angular/core';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { onChange } from '@core/util/signals';

export interface CurrentPage {
  id: number;
  slug: string;
  title: string;
}

@Service()
export class CurrentPageService {
  private readonly open = signal<CurrentPage | undefined>(undefined);

  readonly page = this.open.asReadonly();

  constructor() {
    // The guard switches workspace before the old view is torn down.
    onChange(inject(CurrentWorkspaceService).id, () => this.set(undefined));
  }

  set(page: CurrentPage | undefined) {
    this.open.set(page);
  }

  clearIfCurrent(pageId: number) {
    this.open.update((current) => {
      return current?.id === pageId ? undefined : current;
    });
  }
}
