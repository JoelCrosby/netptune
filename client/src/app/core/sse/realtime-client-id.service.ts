import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class RealtimeClientIdService {
  readonly value = globalThis.crypto.randomUUID();
}
