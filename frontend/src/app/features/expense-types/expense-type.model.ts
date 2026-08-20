export interface ExpenseType {
  id: number;
  name: string;
}

export interface CreateExpenseTypeRequest {
  name: string;
}

export interface UpdateExpenseTypeRequest {
  name: string;
}

export interface ExpenseTypeListParams {
  name?: string;
  pageNumber: number;
  pageSize: number;
}
