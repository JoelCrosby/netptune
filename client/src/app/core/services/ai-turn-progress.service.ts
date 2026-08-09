import { Injectable, signal } from '@angular/core';
import { AiTokenUsage } from '@core/models/ai-conversation';

/** The clock is only ever read to the second, so it ticks no faster. */
const ELAPSED_TICK = 1000;

/** The conversation total only follows once a turn is stored, so a turn in flight counts its own. */
@Injectable({ providedIn: 'root' })
export class AiTurnProgressService {
  readonly elapsedMs = signal(0);
  readonly usage = signal<AiTokenUsage | null>(null);

  private startedAt: number | null = null;
  private timer: ReturnType<typeof setInterval> | null = null;

  start(startedAt: number) {
    this.stopTimer();

    this.startedAt = startedAt;
    this.usage.set(null);
    this.elapsedMs.set(since(startedAt));

    this.timer = setInterval(() => {
      this.elapsedMs.set(since(startedAt));
    }, ELAPSED_TICK);
  }

  stop(): number {
    this.stopTimer();

    const turnTime = this.recordTurnTime();

    this.startedAt = null;

    return turnTime;
  }

  private recordTurnTime(): number {
    const startedAt = this.startedAt;

    if (startedAt === null) {
      return 0;
    }

    const turnTime = since(startedAt);

    this.elapsedMs.set(turnTime);

    return turnTime;
  }

  reset() {
    this.usage.set(null);
    this.elapsedMs.set(0);
  }

  private stopTimer() {
    if (this.timer === null) {
      return;
    }

    clearInterval(this.timer);
    this.timer = null;
  }
}

function since(startedAt: number): number {
  return Math.max(0, Date.now() - startedAt);
}
