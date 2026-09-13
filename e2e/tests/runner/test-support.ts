import { mkdtemp, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import type { Clock } from '../../ports/clock.js';

export class DeterministicClock implements Clock {
  private index = 0;
  public constructor(private readonly moments: readonly string[]) {}
  public now(): Date {
    const value = this.moments[Math.min(this.index, this.moments.length - 1)];
    this.index += 1;
    return new Date(value);
  }
}

export async function withTemporaryDirectory<T>(action: (directory: string) => Promise<T>): Promise<T> {
  const directory = await mkdtemp(join(tmpdir(), 'zuppeto-e2e-runner-'));
  try {
    return await action(directory);
  } finally {
    await rm(directory, { recursive: true, force: true });
  }
}
