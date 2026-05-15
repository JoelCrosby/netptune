import { Injectable, inject } from '@angular/core';
import { LocalStorageService } from '@core/local-storage/local-storage.service';

export interface RecentItem {
  title: string;
  url: string;
  type: string;
}

const STORAGE_KEY = 'netptune.recent';
const MAX_RECENT = 10;

@Injectable({ providedIn: 'root' })
export class RecentItemsService {
  private storage = inject(LocalStorageService);

  getRecent(): RecentItem[] {
    return this.storage.getItem<RecentItem[]>(STORAGE_KEY) ?? [];
  }

  addRecent(item: RecentItem) {
    const current = this.getRecent().filter((r) => r.url !== item.url);
    const updated = [item, ...current].slice(0, MAX_RECENT);
    this.storage.setItem(STORAGE_KEY, updated);
  }

  clearRecent() {
    this.storage.removeItem(STORAGE_KEY);
  }
}
