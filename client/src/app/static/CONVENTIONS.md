# Static components

The shared presentation layer. Everything under `static/` is generic: it knows about
shapes, not about workspaces, tasks or sprints. Feature code composes these; it does
not restyle them.

This file covers only what is specific to this project.

## The rule

**A visible surface belongs to a static component.** If markup outside `static/` sets
a background, border, radius, shadow, ring or hover state, that is a component the
library either already has or should gain.

Raw `div`s and `span`s outside `static/` are for **layout only** — `flex`, `grid`,
`gap`, sizing, spacing, text alignment. Those never need a component.

```html
<!-- layout: fine -->
<div class="flex min-w-0 items-center gap-3">

<!-- surface: reach for the component -->
<div class="border-border bg-card rounded-lg border shadow-sm">   ✗
<section app-panel surface="card">                                ✓
```

Before hand-rolling anything, search `static/components` for the selector. The library
is large and under-known — the commonest failure is not knowing `app-panel`,
`app-badge` or `app-inline-button` exists.

## Reaching for the right one

| You are building | Use |
| --- | --- |
| A raised card or settings block | `app-panel` + `app-panel-header` / `-body` / `-footer` |
| A page shell | `app-page-container`, `app-page-header`, `app-page-body` |
| A small status pill | `app-badge` |
| A button | `app-flat-button`, `app-stroked-button`, `app-icon-button`, `app-inline-button` |
| A text-only action inside a row | `app-inline-button` (`appearance` picks the hover) |
| A form field | `app-form-input`, `app-form-select`, `app-form-textarea` |
| A bare input inside custom chrome | `app-form-control-field` wrapping `input appFormInput` |
| Nothing to show yet | `app-empty-state`, `app-error-state`, `app-skeleton` |
| A working indicator | `app-spinner` for a view, `app-spinner-icon` beside a label |
| Tabs | `app-tab-group` (`variant="strip"` for the compact in-panel row) |
| A pick-one control | `app-segmented-control` |
| A person | `app-avatar` |

## Element vs attribute selectors

Components that are purely a surface take both forms:

```html
<app-panel surface="card">              <!-- plain wrapper -->
<form app-panel surface="card">         <!-- keeps the <form> -->
<header app-panel-body divider="bottom"><!-- keeps the landmark -->
```

Use the attribute form whenever the element carries meaning of its own — a `<form>`,
a `<header>`, a `<footer>`, a landmark. Only a plain `<div>` should become
`<app-panel>`.

## Overriding styles

Every surface component takes a `class` input, merged with `cn()` (tailwind-merge), so
a later class wins over the component's own:

```html
<app-badge color="warn" shape="rounded" class="px-1.5 text-[10px]">
```

Use this for spacing and one-off sizing. Do **not** use it to rebuild a different
variant — add the variant to the component instead, so the next caller finds it.

## Adding to the library

Add a component when a surface appears **three times**, or twice in two different
features. Two occurrences in one file is a local `@for`, not a component.

New variants go on the existing component rather than into a new one. `app-panel`'s
`surface`, `app-tab-group`'s `variant` and `app-inline-button`'s `appearance` all
exist because feature code had quietly grown a second version.

A component that is generic but lives in a feature folder is the failure mode to watch
for: it looks like reuse and prevents it.

## Enum inputs and i18n

`@angular-eslint/template/i18n` flags any static attribute it does not recognise, so a
new fixed-vocabulary input (`surface`, `divider`, `padding`, `variant`, …) must be
added to `ignoreAttributes` in `eslint.config.mjs`. Only fixed vocabularies belong
there — see `src/locale/CONVENTIONS.md`.

Text inputs on these components (`heading`, `description`, `title`, `placeholder`,
`label`, …) are translated with `i18n-<input>` at the call site.

## Dead weight

The library grew faster than adoption, and an unused component is worse than none — it
gives the next person a second answer. If nothing outside its own folder imports it,
delete it.
