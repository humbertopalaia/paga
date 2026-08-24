export interface ExpenseByType {
  typeName: string;
  total: number;
}

export interface DashboardResponse {
  currentBalance: number;
  monthlyIncome: number;
  monthlyExpense: number;
  expensesByType: ExpenseByType[];
}
