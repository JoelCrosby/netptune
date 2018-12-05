export interface IDisposable {

  isDirty: boolean;
  markDirty(): void;

}
