import { Service, signal } from '@angular/core';
import { AiTokenUsage } from '@core/models/ai-conversation';
import { elapsedClock } from '@core/util/elapsed-clock';

/** The conversation total only follows once a turn is stored, so a turn in flight counts its own. */
@Service()
export class AiTurnProgressService {
  private readonly clock = elapsedClock();

  readonly elapsedMs = this.clock.elapsedMs;
  readonly usage = signal<AiTokenUsage | null>(null);

  start(startedAt: number) {
    this.usage.set(null);
    this.clock.start(startedAt);
  }

  stop(): number {
    return this.clock.stop();
  }

  reset() {
    this.usage.set(null);
    this.clock.reset();
  }
}
