# i18n conventions

Source locale `en-GB`; targets `fr`, `de`, `es`. Translations are inlined at build
time, so changing locale is a full page load, not a runtime toggle.

This file covers only what is specific to this project. For `i18n`, `$localize`, ICU
and the rest, use Angular's documentation.

## Workflow

```bash
pnpm i18n:extract   # extracts AND merges into messages.{fr,de,es}.xlf
pnpm i18n:check     # fails if anything is untranslated
```

Extraction uses the `ng-extract-i18n-merge` builder rather than Angular's own, which
writes only the source catalog. Two of its options are load-bearing: `fuzzyMatch`
re-matches a unit by source text when its id changes — message ids hash the message
text *including* surrounding whitespace, so reformatting alone can change one — and
`trim` stays `false`, because trimming would strip the leading space from
`" · edited"` and break that separator.

## What the build enforces

`i18nMissingTranslation: "error"` (production only) fails when a message id is absent
from the catalogs: a string was added or reworded and `pnpm i18n:extract` was never
run.

`pnpm i18n:check` fails when a unit exists with a `<source>` but no `<target>`:
extracted, never translated. **The Angular setting does not cover this** — the build
succeeds and silently ships English. It runs from `pnpm build` and from the
production Docker build.

`docker-dev` and `development-fr` keep `warning` and skip the check, so an
untranslated string falls back to English while you are mid-change.

## Adding strings

`@angular-eslint/template/i18n` runs as an error over every component, inline
templates included, so unmarked template text fails `pnpm lint`.

**It cannot see TypeScript**, nor English inside template expressions
(`{{ done ? 'Yes' : 'No' }}`). Sweep an area's TypeScript before calling it done:

```bash
grep -rnE "(label|title|message|actionTitle|acceptLabel|cancelLabel|placeholder|emptyMessage|description|hint):\s*'[^']+'|\?\s*'[^']+'\s*:\s*'[^']+'|snackbar\.(open|error|success|warn|info)\('" \
  --include='*.ts' src/app/features/<area> | grep -v '\$localize'
```

## Message ids are `text` + `meaning` — never `description`

The most common way to get dedup wrong:

```ts
// ONE unit with ONE shared translation. The differing descriptions do not split it.
$localize`:Page title for the signed-in user profile:Profile`
$localize`:Profile menu item that opens the user's profile:Profile`
```

A `description` is translator documentation only. To force a split, add a `meaning`:

```ts
$localize`:profile menu heading|Heading when the account has no display name:Profile`
```

Dedup is wanted here — the many `Cancel` / `Save` / `Delete` labels should collapse
to one unit each. Add a `description` whenever a string is under ~3 words or its
context is not obvious; add a `meaning` only to split a genuine homonym.

Use generated ids. Reserve `@@custom-id` for long-form copy you expect to reword for
tone without changing meaning.

## Templates

Mark the innermost element holding the complete sentence and nothing else. Buttons
here are `icon + text`, so the usual edit is wrapping the text so the `<svg>` does
not become a placeholder:

```html
<button app-menu-item type="button" (click)="logOut(menu)">
  <svg lucideLogOut class="h-4 w-4 shrink-0"></svg>
  <span i18n="Profile menu item that signs the user out">Logout</span>
</button>
```

Use `<ng-container i18n>` where an extra `<span>` would change layout (flex/grid
children, `gap`, table cells); Prettier also never hugs it.

Keep punctuation against a closing inline tag — `<strong>Public</strong>.` — so a
line break cannot render a space before it.

Translatable static inputs on this repo's shared components: `label`, `title`,
`aria-label`, `description`, `placeholder`, `heading`, `appTooltip`, `hint`,
`emptyMessage`, `errorMessage`, `actionTitle`, `itemLabel`.

You cannot mark a `[binding]` — localize at the TypeScript source. You cannot
translate an attribute whose name starts with `on` (NG5002); use a bound input.

## TypeScript

A colon terminates the description, so a colon *inside* one silently truncates the
message:

```ts
$localize`:Task priority: none:None`  // description "Task priority", message " none:None"
```

`$localize` does not evaluate ICU. Use a ternary over two messages in TypeScript, and
real ICU in templates.

Name every placeholder in a message that has two or more: `${count}:TASK_COUNT:` in
TypeScript, `{{ count // i18n(ph="TASK_COUNT") }}` in templates.

## Do not mark

NgRx action types; permission keys and other API contract values (`'dark'`, status
and kind discriminants); entity and user data (`workspace.name`, `sprint.name`);
route segments and slugs; CSS classes, `id`, `autocomplete`, ARIA *values*;
`KeyboardEvent.key` comparisons; date format patterns; `console.*`.

Locale display names in `core/util/locale.ts` are deliberately untranslated, so
someone stranded in a language they cannot read can still find theirs.

## `Create <Entity>`

Keep the whole label as one message per entity; never `Create {{ entity }}`. Word
order differs — German puts the verb last. Where a generic component takes a dynamic
label, pass an already-localized string from the call site.

## Adding a locale

`supportedLocales` in `core/util/locale.ts` is the single source for each locale's
code, URL prefix and display name. Three things outside TypeScript must be updated to
match: the `i18n` block in `angular.json`, `extract-i18n`'s `targetFiles` there, and
the locale maps in `nginx.conf`.

## Seeing another language locally

`ng serve` is limited to one locale and forces flat output, so `/fr/` there returns
the English bundle and the router reads `fr` as a workspace slug.

```bash
pnpm start:fr    # French dev server: HMR, one locale, no switcher
pnpm docker:dev  # full image incl. nginx locale negotiation, against the Aspire API
```

`docker:dev` uses host networking with `NGINX_PORT=8080`: Aspire binds the API to
`127.0.0.1`, so a bridged container cannot reach it, and rootless Podman cannot bind
port 80.
