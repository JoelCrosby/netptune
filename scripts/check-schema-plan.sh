#!/usr/bin/env bash
# Prints the changes in an Atlas schema plan that are unsafe to apply while the previous release is
# still serving traffic, and exits non-zero if there are any.
set -euo pipefail

plan="$1"

statements="$(grep -v '^--' "$plan" || true)"

# Atlas puts every change to a table on one ALTER TABLE line, so split it into one clause per line.
clauses="$(sed -E 's/, (ADD|ALTER|DROP|RENAME) /\n\1 /g' <<< "$statements")"

awk '
  NR == FNR {
    if (match($0, /^CREATE TABLE "[^"]+"/)) {
      created[substr($0, 15, RLENGTH - 15)] = 1
    }
    next
  }

  /DROP (TABLE|SCHEMA|TYPE|COLUMN|VIEW|FUNCTION)/ || /ALTER COLUMN "[^"]+" TYPE/ || /RENAME/ || /SET NOT NULL/ {
    print
    unsafe = 1
    next
  }

  /ADD COLUMN/ && /NOT NULL/ && !/DEFAULT/ {
    print
    unsafe = 1
    next
  }

  match($0, /^CREATE UNIQUE INDEX "[^"]+" ON "[^"]+"/) {
    table = substr($0, RSTART, RLENGTH)
    sub(/.* ON "/, "", table)
    sub(/"$/, "", table)

    if (!(table in created)) {
      print
      unsafe = 1
    }
  }

  END { exit unsafe }
' <(echo "$statements") <(echo "$clauses")
