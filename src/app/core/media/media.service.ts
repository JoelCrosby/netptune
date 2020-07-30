import { Injectable } from '@angular/core';
import { MediaMatcher } from '@angular/cdk/layout';

@Injectable({
  providedIn: 'root',
})
export class MediaService {
  mobileQuery: MediaQueryList;

  constructor(public media: MediaMatcher) {
    this.mobileQuery = media.matchMedia('(max-width: 600px)');
  }

  addListener(
    listener: ((this: MediaQueryList, ev: MediaQueryListEvent) => void) | null
  ) {
    listener.call(listener, this.mobileQuery);
    this.mobileQuery.addEventListener('change', listener);
  }
}
