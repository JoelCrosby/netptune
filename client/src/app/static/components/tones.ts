export type Tone =
  'neutral' | 'blue' | 'purple' | 'amber' | 'green' | 'teal' | 'pink';

export const toneClasses: Record<Tone, string> = {
  neutral: 'bg-foreground/6 text-foreground/70',
  blue: 'bg-blue-500/10 text-blue-700 dark:text-blue-300',
  purple: 'bg-primary/10 text-primary',
  amber: 'bg-amber-500/12 text-amber-700 dark:text-amber-300',
  green: 'bg-emerald-500/12 text-emerald-700 dark:text-emerald-300',
  teal: 'bg-teal-500/12 text-teal-700 dark:text-teal-300',
  pink: 'bg-pink-500/12 text-pink-700 dark:text-pink-300',
};

const accentTones: readonly Tone[] = [
  'blue',
  'purple',
  'amber',
  'green',
  'teal',
  'pink',
];

export function accentToneFor(seed: number): Tone {
  const index = Math.abs(Math.trunc(seed)) % accentTones.length;

  return accentTones[index];
}
