import type { Comparison } from '~/types/comparison';

export const comparisons: Comparison[] = [
  {
    competitor: 'Open source',
    headline: 'Inspect it, change it, contribute to it.',
    description:
      'The complete application is available under the MIT license, from the Angular client to the .NET services and Helm chart.',
  },
  {
    competitor: 'Self-hosted',
    headline: 'Keep the deployment under your control.',
    description:
      'Run Netptune in Kubernetes with the maintained Helm chart and choose where its database, object storage, and workloads live.',
  },
  {
    competitor: 'Permissions',
    headline: 'Four roles with granular capabilities.',
    description:
      'Owner, Admin, Member, and Viewer defaults are backed by explicit workspace permissions for projects, boards, tasks, members, and more.',
  },
  {
    competitor: 'Traceability',
    headline: 'Understand how work changed.',
    description:
      'Activity records, audit views, and automation run history provide a clear account of changes and background actions.',
  },
];
