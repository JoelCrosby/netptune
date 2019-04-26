export class TextHelpers {
  static truncate(input: string, maxLength = 20) {
    if (input.length >= maxLength) {
      return input;
    }
    return input.substring(0, 20);
  }
}
