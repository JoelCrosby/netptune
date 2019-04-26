export class TextHelpers {
  static truncate(input: string, maxLength = 48) {
    if (input.length <= maxLength) {
      return input;
    }
    return input.substring(0, maxLength - 4) + ' ...';
  }
}
