import { defineConfig, globalIgnores } from 'eslint/config';
import eslint from '@eslint/js';
import angular from 'angular-eslint';
import typescript from 'typescript-eslint';

// 'plugin:@angular-eslint/recommended',
// 'plugin:@angular-eslint/template/process-inline-templates',
// 'eslint:recommended',
// 'plugin:@typescript-eslint/recommended',
// 'plugin:@typescript-eslint/recommended-requiring-type-checking',
// 'plugin:prettier/recommended'

export default defineConfig([
  globalIgnores(['projects/**/*', '**/*.js']),
  {
    files: ['**/*.ts'],

    extends: [
       eslint.configs.recommended,
      // Apply the recommended TypeScript rules
      ...typescript.configs.recommended,
      // Optionally apply stylistic rules from typescript-eslint that improve code consistency
      ...typescript.configs.stylistic,
      // Apply the recommended Angular rules
      ...angular.configs.tsRecommended,
    ],

    languageOptions: {
      ecmaVersion: 5,
      sourceType: 'script',

      parserOptions: {
        project: ['tsconfig.json'],
        createDefaultProgram: true,
      },
    },

    processor: angular.processInlineTemplates,

    rules: {
      '@angular-eslint/component-selector': [
        'error',
        {
          type: 'element',
          prefix: 'app',
          style: 'kebab-case',
        },
      ],

      '@angular-eslint/directive-selector': [
        'error',
        {
          type: 'attribute',
          prefix: 'app',
          style: 'camelCase',
        },
      ],

      '@typescript-eslint/no-explicit-any': 'error',
      '@typescript-eslint/explicit-module-boundary-types': ['off'],
      '@typescript-eslint/no-unsafe-member-access': ['off'],
      '@typescript-eslint/no-unsafe-assignment': ['off'],
      '@typescript-eslint/no-unsafe-call': ['off'],
      '@typescript-eslint/no-unsafe-return': ['off'],

      '@typescript-eslint/no-unused-vars': [
        'error',
        {
          ignoreRestSiblings: true,
          argsIgnorePattern: '^_',
        },
      ],

      '@typescript-eslint/unbound-method': [
        'error',
        {
          ignoreStatic: true,
        },
      ],

      curly: 'off',
      'no-shadow': 'off',
    },
  },
  {
    files: ['**/*.html'],
    extends: [
      ...angular.configs.templateRecommended,
      ...angular.configs.templateAccessibility,
    ],
    rules: {},
  },
]);
