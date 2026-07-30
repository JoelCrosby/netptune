# i18n conventions

How to mark strings for translation in this app. The goal across ~330 components is
**consistency over per-case optimality** — a reviewer should be able to check a
component against this list mechanically.

Source locale is `en-GB`; targets are `fr`, `de`, `es`. Translations are inlined at
build time, so changing locale is a full page load, not a runtime toggle.

## Workflow

```bash
pnpm i18n:extract                 # rewrites src/locale/messages.xlf
node scripts/merge-catalogs.js    # merges it into messages.{fr,de,es}.xlf
```

Always run both. `ng extract-i18n` only writes the source catalog — it does **not**
merge into the locale catalogs, so extraction alone leaves them stale. The merge
script reports `N kept, N untranslated, N removed`; a non-zero "untranslated" count
is your worklist.

Missing translations are a build **warning** and fall back to the English source, so
an incomplete rollout still ships. Track progress with:

```bash
pnpm build 2>&1 | grep -c 'No translation found'
```

## Lint enforcement

`@angular-eslint/template/i18n` **does** reach inline templates — the
`processInlineTemplates` processor in `eslint.config.mjs` surfaces them to template
rules as virtual `.html` files, with line numbers pointing into the `.ts` file. It is
configured as an **error**, scoped by directory to areas that are already done.

When you finish an area, add its glob to the i18n block in `eslint.config.mjs`. That
keeps `pnpm lint` green and makes a regression in a finished area fail the build.

The rule cannot see `$localize` in TypeScript. TS strings are on you and the reviewer.

## Message IDs

**Use generated IDs. Do not author `@@custom-id` as a default.** There were no
catalogs before this work, so custom IDs have nothing to stabilise, and the naming
discipline will not survive a multi-pass rollout. Automatic dedup is a feature here:
the many identical `Cancel` / `Save` / `Delete` labels collapse to one catalog entry
each. And when English copy is reworded, a generated ID *should* invalidate the stale
translation rather than keep serving it.

### The ID is `text` + `meaning` only — NOT description

This is verified behaviour, and it is the most common way to get dedup wrong:

```ts
// These three ALL collapse into ONE catalog unit with ONE shared translation.
// The differing descriptions do not separate them.
$localize`:Page title for the signed-in user profile:Profile`
$localize`:Profile menu item that opens the user's profile:Profile`
$localize`:Heading shown when the account has no display name:Profile`
```

Consequences:

- A `description` is **translator documentation only**. Writing one does not protect
  against an unwanted merge.
- When units merge, only the **first** description survives, while every location is
  listed. The translator sees one description covering several call sites.
- To force a split you **must** add a `meaning` (left of the `|`).

```ts
// Now two units, translatable independently.
$localize`:Profile menu item that opens the user's profile:Profile`
$localize`:profile menu heading|Heading shown when the account has no display name:Profile`
```

Reserve explicit `@@ids` for long-form copy you expect to reword for tone without
changing meaning (e.g. `turnstile.component.ts`'s error paragraphs). Fewer than ~20
app-wide. Scheme: `@@<area>.<component>.<slug>`.

## meaning | description

Syntax: `i18n="meaning|description@@id"`, every part optional.

- **description** — add whenever the string is under ~3 words or its context is not
  obvious from the text. Translators see a flat list; `Open` with no description is a
  coin flip.
- **meaning** — add when the same English text needs different translations in
  different places (homonyms, or a noun used as a label vs. as a stand-in for data).
  This is the *only* thing that splits an ID.

## Templates

### Where `i18n` goes

On the **innermost element containing the complete sentence and nothing else**.
Nearly every button here is `icon + text`, so the most common edit is wrapping the
text in a `<span>` so the `<svg>` does not become a translation placeholder:

```html
<button app-menu-item type="button" (click)="logOut(profileMenu)">
  <svg lucideLogOut class="h-4 w-4 shrink-0"></svg>
  <span i18n="Profile menu item that signs the user out">Logout</span>
</button>
```

Use `<ng-container i18n>` where an extra `<span>` would change layout (flex/grid
children, `gap`, table cells).

### Put `i18n` on an inline element, never a block one

Leading and trailing whitespace inside an `i18n` block becomes **part of the message**,
and Prettier re-indents the contents of block elements (`<div>`, `<p>`, `<button>`,
`<h1>`…) onto their own line. Marking a block element therefore yields `" Logout "`,
which is a *different* message from `"Logout"` — so it will not dedupe, and the
translator sees a padded string.

Prettier leaves inline elements (`<span>`, `<kbd>`, `<a>`) hugging their tags, so the
rule is: **wrap the text in a `<span i18n>` inside the block element.**

```html
<!-- no: extracts as " Logout ", a separate unit from "Logout" -->
<button app-workspace-menu-action i18n="...">Logout</button>

<!-- yes: extracts as "Logout" and dedupes with every other Logout -->
<button app-workspace-menu-action>
  <span i18n="Workspace menu action that signs the user out">Logout</span>
</button>
```

Do not fight Prettier on this — run `pnpm prettier` after each batch and check the
extracted sources are unpadded:

```bash
pnpm i18n:extract && grep -n '<source> \|  </source>' src/locale/messages.xlf
```

- **Never split one sentence across two `i18n` blocks** — word order differs by
  locale. If it spans inline markup, mark the parent and let the markup become a
  placeholder.
- **Never put `@if` / `@for` / `@switch` inside an `i18n` block.** It produces opaque
  nested-template placeholders. Mark the branches individually.

### Attributes

Mark static attributes with `i18n-<attr>`: `label`, `placeholder`, `title`, `alt`,
`aria-label`, plus this repo's component inputs (`heading`, `actionTitle`,
`description`, `emptyMessage`, `errorMessage`, `hint`, `itemLabel`, `acceptLabel`,
`cancelLabel`, `appTooltip`, `subheading`). Static component inputs work — the
runtime applies translated values to inputs, not just DOM attributes.

Two hard limits:

1. **You cannot mark a binding.** `[attr.aria-label]="expr"` and `[label]="expr"`
   have no `i18n-` form. Localize at the TS source instead.
2. **ICU is forbidden in attributes** — it throws at runtime. Build such labels in TS
   with a ternary over two messages.

### ICU plural / select

Every `count === 1 ? 'task' : 'tasks'` in a template becomes ICU. Put the *whole*
clause inside, including the verb (German puts it last), and handle `=0`:

```html
<ng-container i18n="Button that adds the selected tasks to the sprint">
  {selected().length, plural,
    =0 {Add tasks} =1 {Add 1 task} other {Add {{ selected().length }} tasks}}
</ng-container>
```

## TypeScript

Form: ``$localize`:meaning|description@@id:Text with ${value}:NAME: inside` ``.

`$localize` is a global — no import. It is typed via the `@angular/localize` entry in
both `tsconfig.json` and `tsconfig.app.json`.

Use it for anything a template cannot mark: bound attributes, snackbar and dialog
copy, validation messages, route `data.title` / `data.back`, label tables, and chart
options.

```ts
readonly profileMenuLabel = computed(() => {
  const user = this.user();
  const name =
    user?.displayName ||
    user?.email ||
    $localize`:Stands in for the user's name when the account has neither a display name nor an e-mail address:user`;

  // aria-label is a binding, so it cannot be marked with i18n- in the template.
  return $localize`:Accessible label for the button that opens the profile menu. USER_NAME is the display name, e-mail address, or a generic fallback:Open ${name}:USER_NAME: menu`;
});
```

**ICU does not work in `$localize`.** The runtime only concatenates message parts; it
never parses ICU, so `{n, plural, ...}` renders literally. Use a ternary over two
messages, hoist the condition into a named constant (repo convention), and leave a
note:

```ts
const isSingle = count === 1;

// fr/de/es share English's one/other plural split. A locale with more plural
// categories (ru, pl, ar) means moving this string into a template ICU.
return isSingle
  ? $localize`:Confirmation title, deleting one notification:Delete notification?`
  : $localize`:Confirmation title, deleting several notifications:Delete ${count}:COUNT: notifications?`;
```

## Placeholders

Unnamed interpolations extract as `INTERPOLATION_1`, which tells a translator
nothing. Name them.

- Templates: the comment goes **inside** the interpolation —
  `{{ sprint.taskCount // i18n(ph="TOTAL_COUNT") }}`
- TypeScript: the `:NAME:` suffix after the expression.

`SCREAMING_SNAKE_CASE`, describing the **role** (`USER_NAME`, `TASK_COUNT`), never the
variable name. Name every placeholder in any message with two or more.

## Do NOT mark

- NgRx action type strings (`'[Sprints] Create Sprint'`) and any reducer/effect id.
- Enum keys and API contract values: `netptunePermissions.*`, `'dark'` / `'light'`,
  status and kind discriminants, response field names.
- Entity and user data: `user.displayName`, `workspace.name`, `sprint.name`,
  `task.name`, `tag.name`.
- Route path segments, query param names, slugs.
- CSS classes, Tailwind strings, `id`, `autocomplete`, and ARIA *values* from a fixed
  vocabulary (`aria-haspopup="menu"`).
- `console.*` / logger output and dev-only strings.
- `KeyboardEvent.key` comparisons — `'Enter'` / `'Escape'` are key names, not UI text.
  A *rendered* shortcut hint is a separate, markable string.
- Third-party config keys (ApexCharts options, EditorJS tools, Turnstile params) and
  date format patterns.

The lint rule's `ignoreAttributes` list in `eslint.config.mjs` encodes the attribute
half of this. Only add an attribute there when its value is a fixed vocabulary, a CSS
class, an SVG primitive, or an element reference — when in doubt leave it out, so the
rule asks rather than silently skipping real copy.

## `Create <Entity>`

Repo convention is that creation buttons read `Create <Entity>`. Keep the whole label
as **one message per entity** — never `Create {{ entity }}`. French needs an article
and puts the noun after ("Créer un tableau"); German puts the verb last ("Neues Board
erstellen"). There are only ~10 entity types, so discrete messages are cheap and
correct.

Where a shared component takes a dynamic label, pass an **already-localized** string
from the call site rather than composing inside the component.

## Reviewer checklist

1. Every user-visible text node sits in exactly one `i18n` block.
2. No split sentences; no control flow inside a block.
3. Every static translatable attribute has `i18n-<attr>`; no ICU in attributes.
4. Every `[binding]`-supplied label is localized at its TS source.
5. Template `count === 1` is ICU; TS is a ternary with the plural note.
6. Placeholders named; sub-3-word strings have descriptions; homonyms have meanings.
7. Nothing from the do-not-mark list was marked.
8. `pnpm i18n:extract && node scripts/merge-catalogs.js` runs clean, and the new
   entries read sensibly out of context.
9. `npx eslint <area>` is clean if the area is in the enforced glob.
