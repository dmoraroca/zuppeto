export interface ApiRequest {
  readonly method: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  readonly path: string;
  readonly accessToken?: string;
  readonly body?: unknown;
}

export interface ApiResponse<T> {
  readonly status: number;
  readonly body: T;
}

export interface ApiTransport {
  send<T>(request: ApiRequest): Promise<ApiResponse<T>>;
}
