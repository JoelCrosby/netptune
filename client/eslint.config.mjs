import { defineConfig, globalIgnores } from 'eslint/config';
import eslint from '@eslint/js';
import angular from 'angular-eslint';
import typescript from 'typescript-eslint';
import ngrx from '@ngrx/eslint-plugin/v9';

export default defineConfig([
  globalIgnores(['projects/**/*', '**/*.js']),
  {
    files: ['**/*.ts'],

    extends: [
      eslint.configs.recommended,
      ...typescript.configs.recommended,
      ...typescript.configs.stylistic,
      ...angular.configs.tsRecommended,
      ...ngrx.configs.all
    ],

    languageOptions: {
      ecmaVersion: 'latest',
      sourceType: 'module',

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
    rules: {
      '@angular-eslint/template/click-events-have-key-events': 'off',
      '@angular-eslint/template/interactive-supports-focus': 'off',
    },
  },
]);
