export interface Rgb {
  r: number;
  g: number;
  b: number;
}

/** Parses a 3- or 6-digit hex colour, returning `null` if it is not valid. */
export const parseHex = (hex: string): Rgb | null => {
  let value = hex.trim().replace('#', '');

  if (value.length === 3) {
    value = value
      .split('')
      .map((char) => char + char)
      .join('');
  }

  if (value.length !== 6 || !/^[0-9a-f]{6}$/i.test(value)) {
    return null;
  }

  return {
    r: parseInt(value.slice(0, 2), 16),
    g: parseInt(value.slice(2, 4), 16),
    b: parseInt(value.slice(4, 6), 16),
  };
};

export interface PastelOptions {
  /** Multiplier applied to the saturation (0–1, lower is less saturated). */
  saturation: number;
  /** Fraction of the remaining lightness added toward white (0–1). */
  lightness: number;
}

/**
 * Converts a hex colour into an `r, g, b` triplet string suitable for use in a
 * CSS custom property such as `--primary-rgb`. Supports 3- and 6-digit hex and
 * returns `null` for anything that is not a valid hex colour.
 *
 * When `pastel` options are supplied the colour is softened (see
 * {@link toPastel}) so it reads as a muted tone rather than a vivid hue.
 */
export const hexToRgbTriplet = (
  hex: string,
  pastel?: PastelOptions
): string | null => {
  const rgb = parseHex(hex);

  if (!rgb) {
    return null;
  }

  const { r, g, b } = pastel
    ? toPastel(rgb.r, rgb.g, rgb.b, pastel)
    : rgb;

  return `${r}, ${g}, ${b}`;
};

/**
 * Converts a hex colour into an `r, g, b` triplet with its lightness forced to
 * a fixed dark target and its saturation optionally reduced, keeping the hue so
 * the result stays on-brand while guaranteeing good contrast with white text.
 */
export const hexToShadeTriplet = (
  hex: string,
  lightness: number,
  saturation = 1
): string | null => {
  const rgb = parseHex(hex);

  if (!rgb) {
    return null;
  }

  const [h, s] = rgbToHsl(rgb.r, rgb.g, rgb.b);
  const { r, g, b } = hslToRgb(h, s * saturation, lightness);

  return `${r}, ${g}, ${b}`;
};

/**
 * Softens a colour by lowering its saturation and raising its lightness so it
 * reads as a muted pastel rather than a harsh, vivid hue.
 */
export const toPastel = (
  r: number,
  g: number,
  b: number,
  options: PastelOptions
) => {
  const [h, s, l] = rgbToHsl(r, g, b);
  const pastelS = s * options.saturation;
  const pastelL = l + (1 - l) * options.lightness;

  return hslToRgb(h, pastelS, pastelL);
};

export const rgbToHsl = (
  r: number,
  g: number,
  b: number
): [number, number, number] => {
  const rn = r / 255;
  const gn = g / 255;
  const bn = b / 255;

  const max = Math.max(rn, gn, bn);
  const min = Math.min(rn, gn, bn);
  const delta = max - min;
  const l = (max + min) / 2;

  if (delta === 0) {
    return [0, 0, l];
  }

  const s = delta / (1 - Math.abs(2 * l - 1));

  let h: number;
  if (max === rn) {
    h = ((gn - bn) / delta) % 6;
  } else if (max === gn) {
    h = (bn - rn) / delta + 2;
  } else {
    h = (rn - gn) / delta + 4;
  }

  h = (h * 60 + 360) % 360;

  return [h, s, l];
};

export const hslToRgb = (h: number, s: number, l: number) => {
  const c = (1 - Math.abs(2 * l - 1)) * s;
  const x = c * (1 - Math.abs(((h / 60) % 2) - 1));
  const m = l - c / 2;

  let rn: number;
  let gn: number;
  let bn: number;

  if (h < 60) [rn, gn, bn] = [c, x, 0];
  else if (h < 120) [rn, gn, bn] = [x, c, 0];
  else if (h < 180) [rn, gn, bn] = [0, c, x];
  else if (h < 240) [rn, gn, bn] = [0, x, c];
  else if (h < 300) [rn, gn, bn] = [x, 0, c];
  else [rn, gn, bn] = [c, 0, x];

  return {
    r: Math.round((rn + m) * 255),
    g: Math.round((gn + m) * 255),
    b: Math.round((bn + m) * 255),
  };
};
