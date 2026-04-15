import tseslint from "@typescript-eslint/eslint-plugin";
import tsParser from "@typescript-eslint/parser";
import prettier from "eslint-config-prettier";
import solid from "eslint-plugin-solid";

/** @type {import("eslint").Linter.Config[]} */
export default [
  {
    ignores: ["dist/**", ".output/**", ".vinxi/**", "node_modules/**"],
  },
  {
    files: ["**/*.{ts,tsx}"],
    plugins: {
      "@typescript-eslint": tseslint,
      solid,
    },
    languageOptions: {
      parser: tsParser,
      parserOptions: {
        project: "./tsconfig.json",
        jsxPragma: null, // SolidJS does not use React's JSX pragma
      },
    },
    rules: {
      // TypeScript recommended
      ...tseslint.configs["recommended"].rules,

      // Solid recommended
      ...solid.configs["recommended"].rules,

      // TypeScript strictness adjustments
      "@typescript-eslint/no-unused-vars": ["error", { argsIgnorePattern: "^_", varsIgnorePattern: "^_" }],
      "@typescript-eslint/no-explicit-any": "error",
      "@typescript-eslint/consistent-type-imports": ["error", { prefer: "type-imports" }],

      // Solid-specific best practices
      "solid/reactivity": "error",
      "solid/no-destructure": "error",
      "solid/jsx-no-undef": "error",
    },
  },
  // Must be last — disables ESLint rules that conflict with Prettier formatting
  prettier,
];
