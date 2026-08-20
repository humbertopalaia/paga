export interface User {
  id: string;
  name: string;
  email: string;
  createdAt: string;
}

export interface CreateUserRequest {
  name: string;
  email: string;
  password: string;
}

export interface UpdateUserRequest {
  name: string;
  email: string;
  password?: string;
}

export interface UserListParams {
  name?: string;
  email?: string;
  pageNumber: number;
  pageSize: number;
}
