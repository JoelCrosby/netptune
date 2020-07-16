import { toWordCase } from '@core/util/text-helpers';

export enum colors100 {
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

export enum colors500 {
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

export enum colors700 {
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
  '#F44336',
  '#E91E63',
  '#9C27B0',
  '#673AB7',
  '#3F51B5',
  '#2196F3',
  '#03A9F4',
  '#00BCD4',
  '#009688',
  '#4CAF50',
  '#8BC34A',
  '#CDDC39',
  '#FFEB3B',
  '#FFC107',
  '#FF9800',
  '#FF5722',
  '#795548',
  '#9E9E9E',
  '#607D8B',
];

export const colorDictionary = () => {
  return Object.keys(colors500).map((color) => {
    return {
      name: toWordCase(color),
      color: colors500[color],
    };
  });
};
