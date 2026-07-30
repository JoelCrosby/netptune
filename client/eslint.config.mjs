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
    // i18n enforcement, now covering the whole app.
    //
    // The processInlineTemplates processor above surfaces inline templates to
    // template rules as virtual .html files, so this reaches all 300+ components
    // and not just the three that use templateUrl. Unmarked text or a translatable
    // attribute in any new component is a lint error.
    //
    // It cannot see $localize in TypeScript — that part is on the author and the
    // reviewer. See src/locale/CONVENTIONS.md.
    // src/index.html is the host page, not an Angular template.
    files: ['src/app/**/*.html'],
    rules: {
      '@angular-eslint/template/i18n': [
        'error',
        {
          // Message IDs are generated, not hand-authored — see CONVENTIONS.md.
          checkId: false,
          // Every <title> here is an SVG accessible name holding a brand name
          // (github, google, microsoft, Netptune), which must not be translated.
          // Angular's template AST prefixes SVG tags, hence ':svg:' — plain
          // 'title' silently does nothing. Revisit if a <title> carries prose.
          ignoreTags: [':svg:title'],
          // Merged with the rule's own defaults, so only additions go here. Only
          // attributes whose values are a fixed vocabulary, a CSS class, an SVG
          // primitive, or an element reference belong here — when in doubt leave
          // it out, so the rule asks rather than silently skipping real copy.
          ignoreAttributes: [
            // SVG primitives
            'd', 'points', 'vector-effect', 'orient', 'markerUnits',
            'marker-end', 'marker-start',
            // CSS class inputs on this repo's design-system components
            'buttonClass', 'containerClass', 'emptyCellClass', 'rowClass',
            'tableClass',
            // fixed-vocabulary inputs (enum-like), not prose
            'align', 'appearance', 'appTooltipPosition', 'cdkDropListOrientation',
            'colWrap', 'focusMode', 'preserveAspectRatio', 'rowWrap',
            'entityType',
            'enterFrom', 'enterTo', 'leaveFrom', 'leaveTo', 'mode', 'provider',
            'shape', 'size', 'variant', 'xPosition', 'yPosition',
            // ARIA wiring and element references, not user-visible text
            'accept', 'aria-autocomplete', 'aria-controls', 'aria-describedby',
            'aria-haspopup', 'aria-labelledby', 'aria-live', 'aria-orientation',
            'controlId', 'form', 'groupName', 'property', 'rel', 'scope',
            // property-name references (which field of a data object to read)
            'idKey', 'labelKey',
          ],
        },
      ],
    },
  },
]);
