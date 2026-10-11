import {
  LucideBookOpen,
  LucideCalendarCheck,
  LucideCode,
  LucideFileText,
  LucideListChecks,
  LucideRefreshCw,
  LucideUsers,
  type LucideIconInput,
} from '@lucide/angular';
import { Tone } from '@static/components/tones';

// Keys match the server's PageTemplates.
export type PageTemplateKey =
  | 'blank'
  | 'meeting-notes'
  | 'rfc'
  | 'runbook'
  | 'retro'
  | 'spec'
  | 'from-sprint';

export interface PageTemplateOption {
  key: PageTemplateKey;
  title: string;
  description: string;
  icon: LucideIconInput;
  tone: Tone;
}

export const pageTemplates: readonly PageTemplateOption[] = [
  {
    key: 'blank',
    title: $localize`:Template card for an empty page:Blank`,
    description: $localize`:Description of the blank page template:Start from an empty page`,
    icon: LucideFileText,
    tone: 'neutral',
  },
  {
    key: 'meeting-notes',
    title: $localize`:Template card for meeting notes:Meeting notes`,
    description: $localize`:Description of the meeting notes template:Agenda, notes, action items`,
    icon: LucideUsers,
    tone: 'blue',
  },
  {
    key: 'rfc',
    title: $localize`:Template card for a request for comments document:RFC`,
    description: $localize`:Description of the RFC template:Problem, proposal, open questions`,
    icon: LucideCode,
    tone: 'purple',
  },
  {
    key: 'runbook',
    title: $localize`:Template card for an operational runbook:Runbook`,
    description: $localize`:Description of the runbook template:Steps, rollback, owners`,
    icon: LucideBookOpen,
    tone: 'amber',
  },
  {
    key: 'retro',
    title: $localize`:Template card for a sprint retrospective:Retro`,
    description: $localize`:Description of the retro template:Went well, didn't, actions`,
    icon: LucideRefreshCw,
    tone: 'green',
  },
  {
    key: 'spec',
    title: $localize`:Template card for a feature specification:Spec`,
    description: $localize`:Description of the spec template:Goals, requirements, linked tasks`,
    icon: LucideListChecks,
    tone: 'teal',
  },
  {
    key: 'from-sprint',
    title: $localize`:Template card that writes release notes from a sprint:From a sprint`,
    description: $localize`:Description of the from-a-sprint template:Release notes from completed work`,
    icon: LucideCalendarCheck,
    tone: 'pink',
  },
];
