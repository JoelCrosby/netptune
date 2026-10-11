import { Service, signal } from '@angular/core';

export type PageFolderView = 'list' | 'grid';

const storageKey = 'netptune.pages.folderView';

@Service()
export class PageFolderViewService {
  readonly view = signal(readView());

  setView(view: PageFolderView) {
    this.view.set(view);
    writeView(view);
  }
}

function readView(): PageFolderView {
  try {
    return localStorage.getItem(storageKey) === 'grid' ? 'grid' : 'list';
  } catch {
    return 'list';
  }
}

function writeView(view: PageFolderView) {
  try {
    localStorage.setItem(storageKey, view);
  } catch {
    return;
  }
}
