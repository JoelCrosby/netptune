export const fallbackColor = 'slate' as const;

export const namedColors = [
  fallbackColor,
  'red',
  'rose',
  'pink',
  'fuchsia',
  'purple',
  'violet',
  'indigo',
  'blue',
  'sky',
  'cyan',
  'teal',
  'emerald',
  'green',
  'lime',
  'yellow',
  'amber',
  'orange',
] as const;

export type NamedColor = (typeof namedColors)[number];
export type ColorShade = 100 | 500 | 700;

export interface ColorDefinition {
  name: NamedColor;
  label: string;
  swatchClass: string;
}

interface ColorPaletteEntry extends ColorDefinition {
  shades: Record<ColorShade, string>;
  backgroundClass: string;
  pillBackgroundClass: string;
  pillTextClass: string;
}

interface ColorOptions {
  name: NamedColor;
  label: string;
  shades: Record<ColorShade, string>;
  classes: {
    background: string;
    pillBackground: string;
    pillText: string;
  };
}

const colorPalette: Record<NamedColor, ColorPaletteEntry> = {
  slate: color({
    name: fallbackColor,
    label: $localize`:Name of a colour in the colour picker:Slate`,
    shades: { 100: '#f1f5f9', 500: '#64748b', 700: '#334155' },
    classes: {
      background: 'bg-slate-500',
      pillBackground: 'bg-slate-100 dark:bg-slate-500/20',
      pillText: 'text-slate-700 dark:text-slate-300',
    },
  }),
  red: color({
    name: 'red',
    label: $localize`:Name of a colour in the colour picker:Red`,
    shades: { 100: '#fee2e2', 500: '#ef4444', 700: '#b91c1c' },
    classes: {
      background: 'bg-red-500',
      pillBackground: 'bg-red-100 dark:bg-red-500/20',
      pillText: 'text-red-700 dark:text-red-300',
    },
  }),
  rose: color({
    name: 'rose',
    label: $localize`:Name of a colour in the colour picker:Rose`,
    shades: { 100: '#ffe4e6', 500: '#f43f5e', 700: '#be123c' },
    classes: {
      background: 'bg-rose-500',
      pillBackground: 'bg-rose-100 dark:bg-rose-500/20',
      pillText: 'text-rose-700 dark:text-rose-300',
    },
  }),
  pink: color({
    name: 'pink',
    label: $localize`:Name of a colour in the colour picker:Pink`,
    shades: { 100: '#fce7f3', 500: '#ec4899', 700: '#be185d' },
    classes: {
      background: 'bg-pink-500',
      pillBackground: 'bg-pink-100 dark:bg-pink-500/20',
      pillText: 'text-pink-700 dark:text-pink-300',
    },
  }),
  fuchsia: color({
    name: 'fuchsia',
    label: $localize`:Name of a colour in the colour picker:Fuchsia`,
    shades: { 100: '#fae8ff', 500: '#d946ef', 700: '#a21caf' },
    classes: {
      background: 'bg-fuchsia-500',
      pillBackground: 'bg-fuchsia-100 dark:bg-fuchsia-500/20',
      pillText: 'text-fuchsia-700 dark:text-fuchsia-300',
    },
  }),
  purple: color({
    name: 'purple',
    label: $localize`:Name of a colour in the colour picker:Purple`,
    shades: { 100: '#f3e8ff', 500: '#a855f7', 700: '#7e22ce' },
    classes: {
      background: 'bg-purple-500',
      pillBackground: 'bg-purple-100 dark:bg-purple-500/20',
      pillText: 'text-purple-700 dark:text-purple-300',
    },
  }),
  violet: color({
    name: 'violet',
    label: $localize`:Name of a colour in the colour picker:Violet`,
    shades: { 100: '#ede9fe', 500: '#8b5cf6', 700: '#6d28d9' },
    classes: {
      background: 'bg-violet-500',
      pillBackground: 'bg-violet-100 dark:bg-violet-500/20',
      pillText: 'text-violet-700 dark:text-violet-300',
    },
  }),
  indigo: color({
    name: 'indigo',
    label: $localize`:Name of a colour in the colour picker:Indigo`,
    shades: { 100: '#e0e7ff', 500: '#6366f1', 700: '#4338ca' },
    classes: {
      background: 'bg-indigo-500',
      pillBackground: 'bg-indigo-100 dark:bg-indigo-500/20',
      pillText: 'text-indigo-700 dark:text-indigo-300',
    },
  }),
  blue: color({
    name: 'blue',
    label: $localize`:Name of a colour in the colour picker:Blue`,
    shades: { 100: '#dbeafe', 500: '#3b82f6', 700: '#1d4ed8' },
    classes: {
      background: 'bg-blue-500',
      pillBackground: 'bg-blue-100 dark:bg-blue-500/20',
      pillText: 'text-blue-700 dark:text-blue-300',
    },
  }),
  sky: color({
    name: 'sky',
    label: $localize`:Name of a colour in the colour picker:Sky`,
    shades: { 100: '#e0f2fe', 500: '#0ea5e9', 700: '#0369a1' },
    classes: {
      background: 'bg-sky-500',
      pillBackground: 'bg-sky-100 dark:bg-sky-500/20',
      pillText: 'text-sky-700 dark:text-sky-300',
    },
  }),
  cyan: color({
    name: 'cyan',
    label: $localize`:Name of a colour in the colour picker:Cyan`,
    shades: { 100: '#cffafe', 500: '#06b6d4', 700: '#0e7490' },
    classes: {
      background: 'bg-cyan-500',
      pillBackground: 'bg-cyan-100 dark:bg-cyan-500/20',
      pillText: 'text-cyan-700 dark:text-cyan-300',
    },
  }),
  teal: color({
    name: 'teal',
    label: $localize`:Name of a colour in the colour picker:Teal`,
    shades: { 100: '#ccfbf1', 500: '#14b8a6', 700: '#0f766e' },
    classes: {
      background: 'bg-teal-500',
      pillBackground: 'bg-teal-100 dark:bg-teal-500/20',
      pillText: 'text-teal-700 dark:text-teal-300',
    },
  }),
  emerald: color({
    name: 'emerald',
    label: $localize`:Name of a colour in the colour picker:Emerald`,
    shades: { 100: '#d1fae5', 500: '#10b981', 700: '#047857' },
    classes: {
      background: 'bg-emerald-500',
      pillBackground: 'bg-emerald-100 dark:bg-emerald-500/20',
      pillText: 'text-emerald-700 dark:text-emerald-300',
    },
  }),
  green: color({
    name: 'green',
    label: $localize`:Name of a colour in the colour picker:Green`,
    shades: { 100: '#dcfce7', 500: '#22c55e', 700: '#15803d' },
    classes: {
      background: 'bg-green-500',
      pillBackground: 'bg-green-100 dark:bg-green-500/20',
      pillText: 'text-green-700 dark:text-green-300',
    },
  }),
  lime: color({
    name: 'lime',
    label: $localize`:Name of a colour in the colour picker:Lime`,
    shades: { 100: '#ecfccb', 500: '#84cc16', 700: '#4d7c0f' },
    classes: {
      background: 'bg-lime-500',
      pillBackground: 'bg-lime-100 dark:bg-lime-500/20',
      pillText: 'text-lime-700 dark:text-lime-300',
    },
  }),
  yellow: color({
    name: 'yellow',
    label: $localize`:Name of a colour in the colour picker:Yellow`,
    shades: { 100: '#fef9c3', 500: '#eab308', 700: '#a16207' },
    classes: {
      background: 'bg-yellow-500',
      pillBackground: 'bg-yellow-100 dark:bg-yellow-500/20',
      pillText: 'text-yellow-700 dark:text-yellow-300',
    },
  }),
  amber: color({
    name: 'amber',
    label: $localize`:Name of a colour in the colour picker:Amber`,
    shades: { 100: '#fef3c7', 500: '#f59e0b', 700: '#b45309' },
    classes: {
      background: 'bg-amber-500',
      pillBackground: 'bg-amber-100 dark:bg-amber-500/20',
      pillText: 'text-amber-700 dark:text-amber-300',
    },
  }),
  orange: color({
    name: 'orange',
    label: $localize`:Name of a colour in the colour picker:Orange`,
    shades: { 100: '#ffedd5', 500: '#f97316', 700: '#c2410c' },
    classes: {
      background: 'bg-orange-500',
      pillBackground: 'bg-orange-100 dark:bg-orange-500/20',
      pillText: 'text-orange-700 dark:text-orange-300',
    },
  }),
};

export const avatarColors = namedColors.map(
  (colorName) => colorPalette[colorName].shades[500]
);

export const colorDictionary = (): ColorDefinition[] =>
  namedColors.map((colorName) => colorPalette[colorName]);

export function resolveColorName(
  value: string | null | undefined,
  fallback: NamedColor = fallbackColor
): NamedColor {
  const normalized = value?.trim().toLowerCase();

  if (!normalized) return fallback;
  if (namedColors.includes(normalized as NamedColor)) {
    return normalized as NamedColor;
  }

  return fallback;
}

export function colorHex(
  value: string | null | undefined,
  shade: ColorShade = 500
): string {
  return colorPalette[resolveColorName(value)].shades[shade];
}

export function colorBackgroundClass(value: string | null | undefined): string {
  return colorPalette[resolveColorName(value)].backgroundClass;
}

export function colorPillClasses(value: string | null | undefined): string {
  const entry = colorPalette[resolveColorName(value)];

  return `${entry.pillBackgroundClass} ${entry.pillTextClass}`;
}

export function colorSwatchClass(value: string | null | undefined): string {
  return colorPalette[resolveColorName(value)].swatchClass;
}

export function colorTextClass(value: string | null | undefined): string {
  return colorPalette[resolveColorName(value)].pillTextClass;
}

function color(options: ColorOptions): ColorPaletteEntry {
  return {
    name: options.name,
    label: options.label,
    shades: options.shades,
    swatchClass: options.classes.background,
    backgroundClass: options.classes.background,
    pillBackgroundClass: options.classes.pillBackground,
    pillTextClass: options.classes.pillText,
  };
}
