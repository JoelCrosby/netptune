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
      ...typescript.configs.strict,
      ...typescript.configs.stylistic,
      ...angular.configs.tsRecommended,
      ...ngrx.configs.all
    ],

    languageOptions: {
      ecmaVersion: 'latest',
      sourceType: 'module',

      parserOptions: {
        project: ['tsconfig.app.json'],
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
      '@typescript-eslint/no-extraneous-class': ['off'],

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
  {
    // i18n enforcement, expanded one area at a time as the rollout progresses.
    //
    // The processInlineTemplates processor above surfaces inline templates to
    // template rules as virtual .html files, so this reaches all components and
    // not just the three that use templateUrl. Scoping it to finished areas keeps
    // `pnpm lint` green while making a regression in a finished area a hard error.
    //
    // When an area is fully marked up, add it here. See src/locale/CONVENTIONS.md.
    files: ['src/app/shell/**/*.html'],
    rules: {
      '@angular-eslint/template/i18n': [
        'error',
        {
          // Message IDs are generated, not hand-authored — see CONVENTIONS.md.
          checkId: false,
          // Appended to the rule's own 34 defaults, which this replaces. Only
          // attributes whose values are a fixed vocabulary, a CSS class, an SVG
          // primitive, or an element reference belong here — when in doubt leave
          // it out, so the rule asks rather than silently skipping real copy.
          ignoreAttributes: [
            // rule defaults
            'autocomplete', 'charset', 'class', 'color', 'colspan', 'dir',
            'fill', 'for', 'formArrayName', 'formControlName', 'formGroupName',
            'height', 'href', 'id', 'lang', 'list', 'name', 'ngClass',
            'ngProjectAs', 'role', 'routerLink', 'routerLinkActive', 'src',
            'stroke', 'stroke-width', 'style', 'svgIcon', 'tabindex', 'target',
            'type', 'value', 'viewBox', 'width', 'xmlns',
            // SVG primitives
            'd', 'points', 'vector-effect',
            // CSS class inputs on this repo's design-system components
            'buttonClass', 'containerClass', 'emptyCellClass', 'rowClass',
            'tableClass',
            // fixed-vocabulary inputs (enum-like), not prose
            'align', 'appearance', 'appTooltipPosition', 'entityType',
            'enterFrom', 'enterTo', 'leaveFrom', 'leaveTo', 'mode', 'provider',
            'shape', 'size', 'variant', 'xPosition', 'yPosition',
            // ARIA wiring and element references, not user-visible text
            'accept', 'aria-autocomplete', 'aria-controls', 'aria-describedby',
            'aria-haspopup', 'aria-labelledby', 'aria-live', 'controlId',
            'groupName', 'property', 'rel', 'scope',
            // property-name references (which field of a data object to read)
            'idKey', 'labelKey',
          ],
        },
      ],
    },
  },
]);
