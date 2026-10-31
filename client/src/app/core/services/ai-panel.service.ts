import { Dialog } from '@angular/cdk/dialog';
import { Service, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { BrowserSettingsService } from '@core/settings/browser-settings.service';
import { AiDisplayMode } from '@core/models/ai-display-mode';
import {
  DEFAULT_AI_PANEL_WIDTH,
  clampAiPanelWidth,
} from '@core/models/ai-panel-width';
import { AiModelCatalogService } from '@core/services/ai-model-catalog.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { LayoutService } from '@core/services/layout.service';
import { SessionService } from '@core/services/session.service';
import { WorkspaceService } from '@core/services/workspace.service';

const ASSISTANT_PAGE_PATTERN = /^\/[^/]+\/assistant$/;

@Service()
export class AiPanelService {
  private readonly router = inject(Router);
  private readonly settings = inject(BrowserSettingsService);
  private readonly workspace = inject(WorkspaceService);
  private readonly workspaceId = inject(CurrentWorkspaceService).slug;
  private readonly catalog = inject(AiModelCatalogService);
  private readonly layout = inject(LayoutService);
  private readonly dialog = inject(Dialog);

  readonly isAvailable = inject(SessionService).isAssistantAvailable;
  readonly isOpen = signal(false);

  readonly mode = signal<AiDisplayMode>(
    this.settings.get('ai.mode') ?? 'overlay'
  );

  readonly width = signal(this.readWidth());
  readonly isResizing = signal(false);
  readonly hasUnreadReply = signal(false);

  private readonly overDialog = signal(false);
  private readonly raises = signal(0);

  readonly isOverDialog = this.overDialog.asReadonly();
  readonly raiseRequests = this.raises.asReadonly();

  readonly isVisible = computed(() => {
    return this.isOpen() && this.isAvailable();
  });

  // A phone has no room for a docked column and a dialog's backdrop covers it, so docked
  // mode shows as the overlay in those cases without changing the stored preference.
  private readonly displayMode = computed<AiDisplayMode>(() => {
    const isDocked = this.mode() === 'docked';
    const mustFloat = this.layout.isMobileView() || this.overDialog();

    return isDocked && mustFloat ? 'overlay' : this.mode();
  });

  readonly isOverlayOpen = computed(() => {
    return this.isVisible() && this.displayMode() === 'overlay';
  });

  readonly isDocked = computed(() => {
    return this.isVisible() && this.displayMode() === 'docked';
  });

  private readonly transcriptViewers = signal(0);

  constructor() {
    this.dialog.afterAllClosed
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.overDialog.set(false));
  }

  open() {
    if (!this.isAvailable()) {
      return;
    }

    const isDedicated = this.mode() === 'dedicated';

    if (isDedicated) {
      void this.openPage();

      return;
    }

    const hasOpenDialog = this.dialog.openDialogs.length > 0;

    if (hasOpenDialog) {
      this.overDialog.set(true);
      this.raises.update((count) => count + 1);
    }

    this.isOpen.set(true);

    void this.catalog.load();
  }

  close() {
    this.isOpen.set(false);
    this.overDialog.set(false);
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

    const hasOpenDialog = this.dialog.openDialogs.length > 0;
    const isUnderDialog = hasOpenDialog && !this.overDialog();

    if (this.isOpen() && !isUnderDialog) {
      this.close();

      return;
    }

    this.open();
  }

  setMode(mode: AiDisplayMode) {
    this.mode.set(mode);
    this.settings.set('ai.mode', mode);

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
    this.settings.set('ai.panelWidth', this.width());
  }

  private readWidth(): number {
    const stored = this.settings.get('ai.panelWidth');

    if (typeof stored !== 'number') {
      return DEFAULT_AI_PANEL_WIDTH;
    }

    return clampAiPanelWidth(stored);
  }
}
