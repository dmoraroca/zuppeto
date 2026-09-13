import type { ExecutionRecord } from '../domain/execution.js';
export interface ExecutionJournal {
  append(record: ExecutionRecord): Promise<boolean>;
  list(runId: string): Promise<readonly ExecutionRecord[]>;
  has(executionId: string): Promise<boolean>;
}
