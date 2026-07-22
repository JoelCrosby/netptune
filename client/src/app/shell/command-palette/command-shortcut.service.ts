import { DOCUMENT } from '@angular/common';
import { DestroyRef, Injectable, inject } from '@angular/core';
import {
  Command,
  CommandRegistry,
} from '@core/services/command-registry.service';
import { CommandPaletteService } from './command-palette.service';

const SHORTCUT_TIMEOUT_MS = 1_000;

@Injectable()
export class CommandShortcutService {
  private readonly document = inject(DOCUMENT);
  private readonly destroyRef = inject(DestroyRef);
  private readonly registry = inject(CommandRegistry);
  private readonly palette = inject(CommandPaletteService);

  private sequence: string[] = [];
  private resetTimeout: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    this.document.addEventListener('keydown', this.onKeydown);
    this.destroyRef.onDestroy(() => {
      this.document.removeEventListener('keydown', this.onKeydown);
      this.resetSequence();
    });
  }

  private readonly onKeydown = (event: KeyboardEvent): void => {
    if (this.shouldIgnore(event)) {
      this.resetSequence();
      return;
    }

    const key = event.key.toLowerCase();
    const commands = this.registry
      .all()
      .filter((command): command is Command & { shortcut: readonly string[] } =>
        Boolean(command.shortcut?.length)
      );
    const nextSequence = [...this.sequence, key];
    const matchingCommand = commands.find((command) =>
      this.sequencesMatch(command.shortcut, nextSequence)
    );

    if (matchingCommand) {
      event.preventDefault();
      this.resetSequence();
      matchingCommand.execute();
      return;
    }

    const hasPartialMatch = commands.some((command) =>
      this.isSequencePrefix(command.shortcut, nextSequence)
    );

    if (hasPartialMatch) {
      event.preventDefault();
      this.sequence = nextSequence;
      this.scheduleReset();
      return;
    }

    this.resetSequence();

    const startsNewSequence = commands.some((command) =>
      this.isSequencePrefix(command.shortcut, [key])
    );

    if (startsNewSequence) {
      event.preventDefault();
      this.sequence = [key];
      this.scheduleReset();
    }
  };

  private shouldIgnore(event: KeyboardEvent): boolean {
    if (
      this.palette.isOpen() ||
      event.defaultPrevented ||
      event.repeat ||
      event.isComposing ||
      event.ctrlKey ||
      event.metaKey ||
      event.altKey ||
      event.shiftKey
    ) {
      return true;
    }

    const target = event.target;
    if (!(target instanceof HTMLElement)) return false;

    return Boolean(
      target.closest(
        'input, textarea, select, [contenteditable]:not([contenteditable="false"]), [role="dialog"], [aria-modal="true"]'
      )
    );
  }

  private sequencesMatch(
    shortcut: readonly string[],
    sequence: readonly string[]
  ): boolean {
    return (
      shortcut.length === sequence.length &&
      shortcut.every(
        (shortcutKey, index) => shortcutKey.toLowerCase() === sequence[index]
      )
    );
  }

  private isSequencePrefix(
    shortcut: readonly string[],
    sequence: readonly string[]
  ): boolean {
    return (
      shortcut.length > sequence.length &&
      sequence.every(
        (sequenceKey, index) => shortcut[index].toLowerCase() === sequenceKey
      )
    );
  }

  private scheduleReset(): void {
    if (this.resetTimeout) clearTimeout(this.resetTimeout);
    this.resetTimeout = setTimeout(
      () => this.resetSequence(),
      SHORTCUT_TIMEOUT_MS
    );
  }

  private resetSequence(): void {
    this.sequence = [];
    if (!this.resetTimeout) return;

    clearTimeout(this.resetTimeout);
    this.resetTimeout = null;
  }
}
