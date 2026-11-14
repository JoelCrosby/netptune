import { Service, computed, inject, signal } from '@angular/core';
import { AiEffort, AiEffortOption } from '@core/models/ai-effort';
import { ASSISTANT_EFFORT } from '@core/models/user-preferences';
import { AiModelCatalogService } from '@core/services/ai-model-catalog.service';
import { UserPreferencesService } from '@core/services/user-preferences.service';

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
  private readonly preferences = inject(UserPreferencesService);
  private readonly catalog = inject(AiModelCatalogService);

  readonly efforts = computed(() => {
    const available = this.catalog.availableEfforts();

    return EFFORT_OPTIONS.filter((option) => available.includes(option.effort));
  });

  /** The level the user picked, where null means automatic — not the level a conversation resolved to. */
  readonly selectedEffort = signal<AiEffort | null>(this.readPreference());

  // The server clamps a level the model lacks to the nearest one below, so show what will actually run.
  readonly effectiveEffort = computed(() => {
    const selected = this.selectedEffort();

    if (selected === null) {
      return null;
    }

    return clampEffort(this.catalog.availableEfforts(), selected);
  });

  readonly selectedEffortLabel = computed(() => {
    const effective = this.effectiveEffort();
    const option = EFFORT_OPTIONS.find((item) => item.effort === effective);

    if (option) {
      return option.label;
    }

    return $localize`:Effort option that lets the server choose:Automatic`;
  });

  select(effort: AiEffort | null) {
    this.selectedEffort.set(effort);
    this.preferences
      .updateValue(ASSISTANT_EFFORT, 'global', effort)
      .subscribe({ error: () => null });
  }

  use(effort: AiEffort | null) {
    this.selectedEffort.set(effort);
  }

  resetToPreference() {
    this.selectedEffort.set(this.readPreference());
  }

  private readPreference(): AiEffort | null {
    const value = this.preferences.effectiveValueFor(ASSISTANT_EFFORT);

    if (value === null || value === undefined) {
      return null;
    }

    const efforts = EFFORT_OPTIONS.map((option) => option.effort);
    const isEffort = typeof value === 'number' && efforts.includes(value as AiEffort);

    return isEffort ? (value as AiEffort) : null;
  }
}

function clampEffort(
  available: AiEffort[],
  desired: AiEffort
): AiEffort | null {
  if (!available.length) return null;

  const isAvailable = available.includes(desired);

  if (isAvailable) {
    return desired;
  }

  const lower = available.filter((effort) => effort < desired);

  return lower.length ? Math.max(...lower) : Math.min(...available);
}
