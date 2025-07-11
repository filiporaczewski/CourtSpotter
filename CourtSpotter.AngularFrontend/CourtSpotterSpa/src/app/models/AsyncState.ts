export interface AsyncState<T> {
  loading: boolean;
  data: T | null;
  error: string | null;
}
