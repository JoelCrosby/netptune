import { Dictionary } from '@ngrx/entity';
import { avatarColors } from './colors';

const availableColors = [...avatarColors];
const colorDictionary: Dictionary<string> = {};

let availableColorIndex = 0;

export const getColourForKey = (key: string) => {
  if (colorDictionary.hasOwnProperty(key)) {
    return colorDictionary[key];
  }

  colorDictionary[key] = getNextAvailableColor();

  return colorDictionary[key];
};

export const getNextAvailableColor = () => {
  if (availableColorIndex === availableColors.length) {
    availableColorIndex = 0;
  }

  const result = availableColors[availableColorIndex];
  availableColorIndex++;

  return result;
};
