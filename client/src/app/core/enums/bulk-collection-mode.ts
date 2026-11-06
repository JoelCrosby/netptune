// Whether the tags or assignees carried by a bulk edit replace what each task holds, or join it.
export enum BulkCollectionMode {
  replace = 0,
  add = 1,
}

export const bulkCollectionModeOptions = [
  {
    value: BulkCollectionMode.add,
    label: $localize`:Collection edit mode that leaves a task's existing tags or assignees in place:Add to`,
  },
  {
    value: BulkCollectionMode.replace,
    label: $localize`:Collection edit mode that swaps out a task's existing tags or assignees:Replace with`,
  },
];
