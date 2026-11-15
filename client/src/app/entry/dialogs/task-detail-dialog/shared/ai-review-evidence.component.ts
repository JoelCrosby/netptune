import { Component, computed, input } from '@angular/core';
import { AiReviewEvidence } from '@core/models/ai-review';
import {
  DetailListComponent,
  DetailListEntry,
} from '@static/components/detail-list.component';
import { DisclosureComponent } from '@static/components/disclosure.component';

@Component({
  selector: 'app-ai-review-evidence',
  imports: [DetailListComponent, DisclosureComponent],
  host: { class: 'block' },
  template: `
    @if (entries().length) {
      <app-disclosure
        class="mt-2"
        i18n-label="
          Opens the list of sources a review finding rests on, under the finding
        "
        label="Why this">
        <app-detail-list [entries]="entries()" />
      </app-disclosure>
    }
  `,
})
export class AiReviewEvidenceComponent {
  readonly evidence = input<readonly AiReviewEvidence[]>([]);

  protected readonly entries = computed<DetailListEntry[]>(() => {
    return this.evidence().map((item) => ({
      text: item.locator,
      detail: item.detail,
    }));
  });
}
