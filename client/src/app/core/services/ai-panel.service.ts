import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { LocalStorageService } from '@core/local-storage/local-storage.service';
import { AiDisplayMode } from '@core/models/ai-display-mode';
import {
  DEFAULT_AI_PANEL_WIDTH,
  clampAiPanelWidth,
} from '@core/models/ai-panel-width';
import { AiModelCatalogService } from '@core/services/ai-model-catalog.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { SessionService } from '@core/services/session.service';
import { WorkspaceService } from '@core/services/workspace.service';

const MODE_STORAGE_KEY = 'ai-assistant.mode';
const PANEL_WIDTH_STORAGE_KEY = 'ai-assistant.panel-width';
const ASSISTANT_PAGE_PATTERN = /^\/[^/]+\/assistant$/;

@Injectable({ providedIn: 'root' })
export class AiPanelService {
  private readonly router = inject(Router);
  private readonly storage = inject(LocalStorageService);
  private readonly workspace = inject(WorkspaceService);
  private readonly workspaceId = inject(CurrentWorkspaceService).slug;
  private readonly catalog = inject(AiModelCatalogService);

  readonly isAvailable = inject(SessionService).isAssistantAvailable;
  readonly isOpen = signal(false);

  readonly mode = signal<AiDisplayMode>(
    this.storage.getItem<AiDisplayMode>(MODE_STORAGE_KEY) ?? 'overlay'
  );

  readonly width = signal(this.readWidth());
  readonly isResizing = signal(false);
  readonly hasUnreadReply = signal(false);

  readonly isVisible = computed(() => {
    return this.isOpen() && this.isAvailable();
  });

  readonly isOverlayOpen = computed(() => {
    return this.isVisible() && this.mode() === 'overlay';
  });

  readonly isDocked = computed(() => {
    return this.isVisible() && this.mode() === 'docked';
  });

  private readonly transcriptViewers = signal(0);

  open() {
    if (!this.isAvailable()) {
      return;
    }

    const isDedicated = this.mode() === 'dedicated';

    if (isDedicated) {
      void this.openPage();

      return;
    }

    this.isOpen.set(true);

    void this.catalog.load();
  }

  close() {
    this.isOpen.set(false);
  }

  toggle() {
    if (!this.isAvailable()) {
      return;
    }

    const isDedicated = this.mode() === 'dedicated';

    if (isDedicated) {
      void this.togglePage();

      return;
    }

    this.isOpen.update((value) => !value);

    if (this.isOpen()) {
      void this.catalog.load();
    }
  }

  setMode(mode: AiDisplayMode) {
    this.mode.set(mode);
    this.storage.setItem(MODE_STORAGE_KEY, mode);

    if (mode === 'dedicated') {
      this.isOpen.set(false);

      void this.openPage();

      return;
    }

    if (this.isOnPage()) {
      void this.leavePage();
    }

    this.open();
  }

  restoreOpen(wasOpen: boolean) {
    const canRestore = wasOpen && this.mode() !== 'dedicated';

    if (!canRestore) {
      return;
    }

    this.isOpen.set(true);
  }

  setWidth(width: number) {
    this.width.set(clampAiPanelWidth(width));

    if (this.isResizing()) {
      return;
    }

    this.persistWidth();
  }

  setResizing(isResizing: boolean) {
    this.isResizing.set(isResizing);

    if (isResizing) {
      return;
    }

    this.persistWidth();
  }

  /**
   * The panel registers itself while it is on screen, so a reply that lands
   * behind a closed chat is the only one that raises the badge.
   */
  watchTranscript(): () => void {
    this.transcriptViewers.update((count) => count + 1);
    this.hasUnreadReply.set(false);

    return () => {
      this.transcriptViewers.update((count) => count - 1);
    };
  }

  markReplyReceived() {
    const isWatched = this.transcriptViewers() > 0;

    if (isWatched) {
      return;
    }

    this.hasUnreadReply.set(true);
  }

  clearUnreadReply() {
    this.hasUnreadReply.set(false);
  }

  private isOnPage(): boolean {
    const path = this.router.url.split('?')[0];

    return ASSISTANT_PAGE_PATTERN.test(path);
  }

  private workspaceRoute(): string | null {
    return this.workspace.getWorkspaceRoute() ?? this.workspaceId() ?? null;
  }

  private async openPage() {
    const workspace = this.workspaceRoute();

    if (!workspace) {
      return;
    }

    await this.catalog.load();
    await this.router.navigate(['/', workspace, 'assistant']);
  }

  private async togglePage() {
    if (this.isOnPage()) {
      await this.leavePage();

      return;
    }

    await this.openPage();
  }

  private async leavePage() {
    const workspace = this.workspaceRoute();

    if (!workspace) {
      return;
    }

    await this.router.navigate(['/', workspace]);
  }

  private persistWidth() {
    this.storage.setItem(PANEL_WIDTH_STORAGE_KEY, this.width());
  }

  private readWidth(): number {
    const stored = this.storage.getItem<number>(PANEL_WIDTH_STORAGE_KEY);

    if (typeof stored !== 'number') {
      return DEFAULT_AI_PANEL_WIDTH;
    }

    return clampAiPanelWidth(stored);
  }
}
