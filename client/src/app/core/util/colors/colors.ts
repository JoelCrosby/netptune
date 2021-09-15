import { toWordCase } from '@core/util/text-helpers';

export enum Colors100 {
  red = '#FFCDD2',
  pink = '#F8BBD0',
  purple = '#E1BEE7',
  deepPurple = '#D1C4E9',
  indigo = '#C5CAE9',
  blue = '#BBDEFB',
  lightBlue = '#B3E5FC',
  cyan = '#B2EBF2',
  teal = '#B2DFDB',
  green = '#C8E6C9',
  lightGreen = '#DCEDC8',
  lime = '#F0F4C3',
  yellow = '#FFF9C4',
  amber = '#FFECB3',
  orange = '#FFE0B2',
  deepOrange = '#FFCCBC',
  brown = '#D7CCC8',
  grey = '#F5F5F5',
  blueGrey = '#CFD8DC',
}

export enum Colors500 {
  red = '#F44336',
  pink = '#E91E63',
  purple = '#9C27B0',
  deepPurple = '#673AB7',
  indigo = '#3F51B5',
  blue = '#2196F3',
  lightBlue = '#03A9F4',
  cyan = '#00BCD4',
  teal = '#009688',
  green = '#4CAF50',
  lightGreen = '#8BC34A',
  lime = '#CDDC39',
  yellow = '#FFEB3B',
  amber = '#FFC107',
  orange = '#FF9800',
  deepOrange = '#FF5722',
  brown = '#795548',
  grey = '#9E9E9E',
  blueGrey = '#607D8B',
}

export enum Colors700 {
  red = '#D32F2F',
  pink = '#C2185B',
  purple = '#4A148C',
  deepPurple = '#512DA8',
  indigo = '#303F9F',
  blue = '#1976D2',
  lightBlue = '#0288D1',
  cyan = '#0097A7',
  teal = '#00796B',
  green = '#388E3C',
  lightGreen = '#689F38',
  lime = '#AFB42B',
  yellow = '#FBC02D',
  amber = '#FFA000',
  orange = '#F57C00',
  deepOrange = '#E64A19',
  brown = '#5D4037',
  grey = '#616161',
  blueGrey = '#455A64',
}

export const avatarColors = [
  '#673AB7',
  '#4CAF50',
  '#FFC107',
  '#F44336',
  '#E91E63',
  '#9C27B0',
  '#FF5722',
  '#3F51B5',
  '#009688',
  '#2196F3',
  '#FFEB3B',
  '#9E9E9E',
  '#795548',
  '#03A9F4',
  '#607D8B',
  '#00BCD4',
  '#8BC34A',
  '#CDDC39',
  '#FF9800',
];

export interface NamedColor {
  name: string;
  color: string;
}

export const colorDictionary = (): NamedColor[] => {
  const pre = Object.keys(Colors500).map((colorKey) => {
    const name = toWordCase(colorKey);
    const color = (Colors500 as { [key: string]: string })[colorKey];

    if (!name || !color) return null;

    const item: NamedColor = {
      name,
      color,
    };

    return item;
  });

  const result: NamedColor[] = [];

  for (const c of pre) {
    if (!c) continue;

    result.push(c);
  }

  return result;
};
