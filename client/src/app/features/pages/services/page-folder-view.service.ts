import { Service, inject, signal } from '@angular/core';
import { BrowserSettingsService } from '@core/settings/browser-settings.service';

export type PageFolderView = 'list' | 'grid';

@Service()
export class PageFolderViewService {
  private readonly settings = inject(BrowserSettingsService);

  readonly view = signal<PageFolderView>(
    this.settings.get('pages.folderView') === 'grid' ? 'grid' : 'list'
  );

  setView(view: PageFolderView) {
    this.view.set(view);
    this.settings.set('pages.folderView', view);
  }
}
