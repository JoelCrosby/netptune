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

export type SignalTone = 'good' | 'warn' | 'bad' | 'muted';

export const signalToneText: Record<SignalTone, string> = {
  good: 'text-emerald-700 dark:text-emerald-400',
  warn: 'text-amber-700 dark:text-amber-300',
  bad: 'text-red-600 dark:text-red-400',
  muted: 'text-muted',
};

export const signalToneSoft: Record<SignalTone, string> = {
  good: 'bg-emerald-500/12 text-emerald-700 dark:text-emerald-400',
  warn: 'bg-amber-500/12 text-amber-700 dark:text-amber-300',
  bad: 'bg-red-500/10 text-red-600 dark:text-red-400',
  muted: 'bg-foreground/6 text-foreground/70',
};

export const signalToneDot: Record<SignalTone, string> = {
  good: 'bg-emerald-500',
  warn: 'bg-amber-500',
  bad: 'bg-red-500',
  muted: 'bg-foreground/40',
};
