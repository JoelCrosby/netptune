import {
  maxLength,
  required,
  schema,
  validate,
} from '@angular/forms/signals';

export interface RequiredTextSchemaOptions {
  label: string;
  maxLength?: number;
  minLength?: number;
}

export function requiredTextSchema({
  label,
  maxLength: maximumLength,
  minLength: minimumLength,
}: RequiredTextSchemaOptions) {
  return schema<string>((field) => {
    required(field, { message: `${label} is required.` });
    validate(field, ({ value }) => {
      const text = value();

      if (!text) return undefined;

      const trimmedText = text.trim();

      if (!trimmedText) {
        return { kind: 'whitespace', message: `${label} is required.` };
      }

      if (
        minimumLength !== undefined &&
        trimmedText.length < minimumLength
      ) {
        return {
          kind: 'minLength',
          message: `${label} must have at least ${minimumLength} characters.`,
        };
      }

      return undefined;
    });

    if (maximumLength !== undefined) {
      maxLength(field, maximumLength, {
        message: `${label} cannot exceed ${maximumLength} characters.`,
      });
    }
  });
}
