import { Dictionary } from '@ngrx/entity';
import { avatarColors } from './colors';

const availableColors = [...avatarColors];
const colorDictionary: Dictionary<string> = {};

const colorIndex = {
  availableColorIndex: 0,
};

export const getColourForKey = (key: string) => {
  console.log(key);

  if (colorDictionary.hasOwnProperty(key)) {
    return colorDictionary[key];
  }

  colorDictionary[key] = getNextAvailableColor();

  return colorDictionary[key];
};

export const getNextAvailableColor = () => {
  if (colorIndex.availableColorIndex === availableColors.length) {
    colorIndex.availableColorIndex = 0;
  }

  const result = availableColors[colorIndex.availableColorIndex];
  colorIndex.availableColorIndex++;

  console.log({ colorIndex });

  return result;
};
