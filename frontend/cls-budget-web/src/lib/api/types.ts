export interface ApiResponse<T> {
  success: boolean;
  data: T | null;
  errors: string[];
}
