import { Component, computed, input } from '@angular/core';
import { AiReviewToolCall } from '@core/models/ai-review';
import {
  aiReviewUnreadSources,
  aiReviewUnreadSourcesLabel,
} from '@core/util/ai-review-copy';
import {
  DetailListComponent,
  DetailListEntry,
} from '@static/components/detail-list.component';
import { DisclosureComponent } from '@static/components/disclosure.component';

// Sits above the findings, because what the review could not read is what the
// findings might have missed.
@Component({
  selector: 'app-ai-review-unread-sources',
  imports: [DetailListComponent, DisclosureComponent],
  host: { class: 'contents' },
  template: `
    @if (entries().length) {
      <app-disclosure [label]="label()">
        <app-detail-list [entries]="entries()" />
      </app-disclosure>
    }
  `,
})
export class AiReviewUnreadSourcesComponent {
  readonly toolCalls = input<readonly AiReviewToolCall[]>([]);

  protected readonly entries = computed<DetailListEntry[]>(() => {
    return aiReviewUnreadSources([...this.toolCalls()]).map((source) => ({
      text: source.label,
      note: source.reason,
    }));
  });

  protected readonly label = computed(() => {
    return aiReviewUnreadSourcesLabel(this.entries().length);
  });
}
