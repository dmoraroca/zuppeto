import type { CleanupCoordinator } from './cleanup-coordinator.js';

export class OriginalStateRestorer<T> {
  public constructor(
    private readonly resource: string,
    private readonly read: () => Promise<T>,
    private readonly write: (state: T) => Promise<void>,
    private readonly cleanup: CleanupCoordinator
  ) {}

  public async capture(): Promise<T> {
    const original = await this.read();
    this.cleanup.register({ resource: `restore:${this.resource}`, cleanup: async () => this.write(original) });
    return original;
  }
}
