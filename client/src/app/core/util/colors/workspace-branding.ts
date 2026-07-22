import {
  hexToRgbTriplet,
  hexToShadeTriplet,
  PastelOptions,
} from './color-conversion';
import { colorHex } from './colors';

const LIGHT_PASTEL: PastelOptions = { saturation: 0.85, lightness: 0.04 };
const DARK_PASTEL: PastelOptions = { saturation: 1.0, lightness: 0.5 };

const SIDE_BAR_LIGHTNESS = 0.16;
const SIDE_BAR_ACTIVE_LIGHTNESS = 0.24;
const SIDE_BAR_SATURATION = 0.2;

export const workspaceBrandVariables = (
  color: string | undefined,
  isDark: boolean
): Record<string, string | null> => {
  if (!color) {
    return {
      '--primary-rgb': null,
      '--side-bar-rgb': null,
      '--side-bar-active-rgb': null,
    };
  }

  const resolvedColor = colorHex(color);

  return {
    '--primary-rgb': hexToRgbTriplet(
      resolvedColor,
      isDark ? DARK_PASTEL : LIGHT_PASTEL
    ),
    '--side-bar-rgb': isDark
      ? null
      : hexToShadeTriplet(
          resolvedColor,
          SIDE_BAR_LIGHTNESS,
          SIDE_BAR_SATURATION
        ),
    '--side-bar-active-rgb': isDark
      ? null
      : hexToShadeTriplet(
          resolvedColor,
          SIDE_BAR_ACTIVE_LIGHTNESS,
          SIDE_BAR_SATURATION
        ),
  };
};
