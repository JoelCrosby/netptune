import { Service } from '@angular/core';

@Service()
export class RealtimeClientIdService {
  readonly value = globalThis.crypto.randomUUID();
}
