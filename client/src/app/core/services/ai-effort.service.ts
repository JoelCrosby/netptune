import { Service, computed, inject, signal } from '@angular/core';
import { LocalStorageService } from '@core/local-storage/local-storage.service';
import { AiEffort, AiEffortOption } from '@core/models/ai-effort';

const EFFORT_STORAGE_KEY = 'ai-assistant.effort';

const EFFORT_OPTIONS: AiEffortOption[] = [
  {
    effort: AiEffort.low,
    label: $localize`:assistant effort level|Assistant reasoning effort, lowest:Low`,
  },
  {
    effort: AiEffort.medium,
    label: $localize`:assistant effort level|Assistant reasoning effort, moderate:Medium`,
  },
  {
    effort: AiEffort.high,
    label: $localize`:assistant effort level|Assistant reasoning effort, the API default:High`,
  },
  {
    effort: AiEffort.xHigh,
    label: $localize`:assistant effort level|Assistant reasoning effort, above high:Extra high`,
  },
  {
    effort: AiEffort.max,
    label: $localize`:assistant effort level|Assistant reasoning effort, the highest:Max`,
  },
];

@Service()
export class AiEffortService {
  private readonly storage = inject(LocalStorageService);

  readonly efforts = EFFORT_OPTIONS;

  /** The level the user picked, where null means automatic — not the level a conversation resolved to. */
  readonly selectedEffort = signal<AiEffort | null>(this.readPreference());

  readonly selectedEffortLabel = computed(() => {
    const selected = this.selectedEffort();
    const option = EFFORT_OPTIONS.find((item) => item.effort === selected);

    if (option) {
      return option.label;
    }

    return $localize`:Effort option that lets the server choose:Automatic`;
  });

  select(effort: AiEffort | null) {
    this.selectedEffort.set(effort);
    this.storage.setItem(EFFORT_STORAGE_KEY, effort);
  }

  use(effort: AiEffort | null) {
    this.selectedEffort.set(effort);
  }

  resetToPreference() {
    this.selectedEffort.set(this.readPreference());
  }

  private readPreference(): AiEffort | null {
    return this.storage.getItem<AiEffort | null>(EFFORT_STORAGE_KEY) ?? null;
  }
}
