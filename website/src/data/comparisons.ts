import type { Comparison } from '~/types/comparison';

export const comparisons: Comparison[] = [
  {
    competitor: 'Open source',
    headline: 'Read the implementation, not a promise.',
    description:
      'The complete application is available under the MIT license, from the Angular client to the .NET services and Helm chart.',
  },
  {
    competitor: 'Infrastructure owned',
    headline: 'Deploy it where your team already operates.',
    description:
      'Use the maintained Kubernetes chart and choose where the workloads, PostgreSQL database, search index, and object storage live.',
  },
  {
    competitor: 'Defaults included',
    headline: 'Useful immediately, adaptable when needed.',
    description:
      'Workspace templates, boards, sprints, reports, automations, search, and permissions work together without requiring a plugin marketplace.',
  },
  {
    competitor: 'Observable behavior',
    headline: 'Trace what changed and what ran.',
    description:
      'Activity records, audit views, and automation run history provide a clear account of changes and background actions.',
  },
  {
    competitor: 'Integration ready',
    headline: 'Automate without sharing a human login.',
    description:
      'Scoped service accounts, revocable credentials, OpenAPI documentation, and an independently scalable public API keep integrations explicit.',
  },
];
