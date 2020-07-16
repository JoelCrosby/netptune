import { Dictionary } from '@ngrx/entity';
import { avatarColors } from './colors';

class ColorUtil {
  readonly availableColors = [...avatarColors];
  readonly colorDictionary: Dictionary<string> = {};

  availableColorIndex = 0;

  getColourForKey(key: string) {
    if (this.colorDictionary.hasOwnProperty(key)) {
      return this.colorDictionary[key];
    }

    this.colorDictionary[key] = this.getNextAvailableColor();

    return this.colorDictionary[key];
  }

  getNextAvailableColor() {
    if (this.availableColorIndex === this.availableColors.length) {
      this.availableColorIndex = 0;
    }

    const result = this.availableColors[this.availableColorIndex];
    this.availableColorIndex++;

    return result;
  }
}

export const avatarColourUtil = new ColorUtil();
