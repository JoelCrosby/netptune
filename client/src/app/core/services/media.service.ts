import { BreakpointObserver, BreakpointState } from '@angular/cdk/layout';
import { Service, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map, distinctUntilChanged } from 'rxjs/operators';

// Each value is the last pixel below the Tailwind breakpoint of the same name, so a
// max-width query here flips at exactly the width the matching `md:`/`lg:` variant does.
export enum MediaSize {
  sm = '639.98px',
  md = '767.98px',
  lg = '1023.98px',
  xl = '1279.98px',
}

@Service()
export class MediaService {
  private breakpointObserver = inject(BreakpointObserver);

  minWidth(mediaSize: MediaSize): Observable<boolean> {
    return this.breakpointObserver.observe([`(min-width: ${mediaSize})`]).pipe(
      map((res) => res.matches),
      distinctUntilChanged()
    );
  }

  maxWidth(mediaSize: MediaSize): Observable<boolean> {
    return this.breakpointObserver.observe([`(max-width: ${mediaSize})`]).pipe(
      map((res) => res.matches),
      distinctUntilChanged()
    );
  }

  matches(query: string): Observable<boolean> {
    return this.breakpointObserver.observe([query]).pipe(
      map((res) => res.matches),
      distinctUntilChanged()
    );
  }

  matchesExact(
    sizeInPixels: number,
    query: 'max-width' | 'min-width' = 'max-width'
  ): Observable<boolean> {
    return this.breakpointObserver
      .observe([`(${query}: ${sizeInPixels}px)`])
      .pipe(
        map((res) => res.matches),
        distinctUntilChanged()
      );
  }

  getMobileQuery(mediaSize: MediaSize): Observable<BreakpointState> {
    return this.breakpointObserver.observe([mediaSize]);
  }
}
