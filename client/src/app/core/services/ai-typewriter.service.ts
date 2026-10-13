import { Service, inject } from '@angular/core';
import { AiTranscriptService } from '@core/services/ai-transcript.service';
import { Typewriter } from '@core/util/typewriter';

@Service()
export class AiTypewriterService {
  private readonly transcript = inject(AiTranscriptService);
  private readonly typewriter = new Typewriter((text) => {
    this.transcript.appendText(text);
  });

  push(text: string) {
    this.typewriter.push(text);
  }

  /** Ends the reveal early with everything received, for a turn the user stopped. */
  flush() {
    this.typewriter.flush();
  }

  /** Drops text whose reply is being discarded or replaced, so it never reaches the transcript. */
  discard() {
    this.typewriter.discard();
  }
}
