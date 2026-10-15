export interface Income {
  id: number;
  date: string;           // yyyy-MM-dd
  description: string;
  value: number;
  isRecurring: boolean;
  frequency: string | null; // 'weekly' | 'monthly' | 'yearly' | null
  isGenerated: boolean;
  sourceEntryId: number | null;
}

export interface CreateIncomeRequest {
  date: string;
  description: string;
  value: number;
  isRecurring: boolean;
  frequency: string | null;
}

export interface UpdateIncomeRequest {
  date: string;
  description: string;
  value: number;
  isRecurring: boolean;
  frequency: string | null;
}

export interface IncomeListParams {
  dateFrom?: string;
  dateTo?: string;
  description?: string;
  isRecurring?: boolean;
  isGenerated?: boolean;
  pageNumber: number;
  pageSize: number;
}
