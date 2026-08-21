export interface Expense {
  id: number;
  dueDate: string;           // yyyy-MM-dd
  description: string;
  expenseTypeId: number;
  expenseTypeName: string;
  value: number;
  isRecurring: boolean;
  frequency: string | null;  // 'weekly' | 'monthly' | 'yearly' | null
}

export interface CreateExpenseRequest {
  dueDate: string;
  description: string;
  expenseTypeId: number;
  value: number;
  isRecurring: boolean;
  frequency: string | null;
}

export interface UpdateExpenseRequest {
  dueDate: string;
  description: string;
  expenseTypeId: number;
  value: number;
  isRecurring: boolean;
  frequency: string | null;
}

export interface ExpenseListParams {
  dueDateFrom?: string;
  dueDateTo?: string;
  expenseTypeId?: number;
  description?: string;
  isRecurring?: boolean;
  pageNumber: number;
  pageSize: number;
}
