export interface FeatureGroupItem {
  title: string;
  description: string;
}

export interface FeatureGroup {
  name: string;
  tone: 'dark' | 'light';
  wide?: boolean;
  background: string;
  orb: string;
  items: FeatureGroupItem[];
}

export const featureGroups: FeatureGroup[] = [
  {
    name: 'Plan',
    tone: 'dark',
    wide: true,
    background: 'bg-ink',
    orb: 'bg-[radial-gradient(circle,rgb(139_92_246/0.55),transparent_65%)]',
    items: [
      { title: 'Kanban boards', description: 'Drag work across columns and watch it update live' },
      { title: 'Tasks on several boards', description: 'One task, wherever it needs to be seen' },
      { title: 'Sprints', description: 'Plan, start and wrap up work in time-boxes' },
      { title: 'Calendar', description: 'Every due date on one monthly view' },
      { title: 'Roadmap', description: 'The bigger picture on a timeline you can edit' },
      { title: 'Saved queries', description: 'Build a view once and share it with the team' },
    ],
  },
  {
    name: 'Write',
    tone: 'light',
    background: 'bg-white',
    orb: 'bg-[radial-gradient(circle,rgb(217_70_239/0.18),transparent_65%)]',
    items: [
      { title: 'Pages', description: 'Docs, notes and runbooks next to the work' },
      { title: 'Templates', description: 'Start workspaces and pages from something good' },
      { title: 'Task review', description: 'Let the assistant spot the gaps before work starts' },
      { title: 'File attachments', description: 'Keep the screenshots and specs on the task' },
      {
        title: 'Search everything',
        description: 'Tasks, pages and projects in one search, archive included',
      },
    ],
  },
  {
    name: 'Ship',
    tone: 'dark',
    background: 'bg-linear-160 from-brand via-violet-500 via-55% to-fuchsia-600',
    orb: 'bg-[radial-gradient(circle,rgb(255_255_255/0.18),transparent_65%)]',
    items: [
      {
        title: 'GitHub integration',
        description: 'Branches and commits find their own way to tasks',
      },
      {
        title: 'Pull requests on tasks',
        description: 'See merged, open and failing checks at a glance',
      },
      { title: 'API', description: 'Connect your other tools with their own keys' },
      {
        title: 'Bring your work with you',
        description: 'Import from spreadsheets, Jira, Trello or Asana',
      },
      {
        title: 'Export any time',
        description: 'Take a full copy of your workspace whenever you like',
      },
    ],
  },
  {
    name: 'Stay in control',
    tone: 'light',
    background: 'bg-white',
    orb: 'bg-[radial-gradient(circle,rgb(125_211_252/0.25),transparent_65%)]',
    items: [
      { title: 'Activity history', description: 'Who changed what, and when' },
      {
        title: 'Roles and permissions',
        description: 'Owner, admin, member and viewer, fine-tuned per person',
      },
      { title: 'Public workspaces', description: 'Share a read-only workspace with anyone' },
      {
        title: 'Notifications your way',
        description: 'Choose what you hear about, workspace by workspace',
      },
    ],
  },
  {
    name: 'Your way',
    tone: 'light',
    background: 'bg-[#e9e1f7]',
    orb: 'bg-[radial-gradient(circle,rgb(103_58_183/0.2),transparent_65%)]',
    items: [
      { title: 'Sign in your way', description: 'GitHub, Google, Microsoft or email' },
      { title: 'Four languages', description: 'English, French, German and Spanish' },
      { title: 'Works on phones', description: 'Check in and move work from anywhere' },
      { title: 'Keyboard shortcuts', description: 'Jump anywhere without touching the mouse' },
    ],
  },
];
