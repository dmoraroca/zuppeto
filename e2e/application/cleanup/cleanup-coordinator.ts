export interface CleanupIssue {
  readonly category: 'CLEANUP';
  readonly resource: string;
  readonly message: string;
}

export interface CleanupRegistration {
  readonly resource: string;
  cleanup(): Promise<void>;
}

export interface CleanupReporter {
  report(issue: CleanupIssue): void;
}

export class CleanupCoordinator {
  private readonly registrations: CleanupRegistration[] = [];
  private completed = false;

  public constructor(private readonly reporter: CleanupReporter = { report: () => undefined }) {}

  public register(registration: CleanupRegistration): void {
    if (this.completed) throw new Error('No es pot registrar cleanup després de completar-lo.');
    if (this.registrations.some((item) => item.resource === registration.resource)) {
      throw new Error(`Cleanup duplicat per al recurs exacte ${registration.resource}.`);
    }
    this.registrations.push(registration);
  }

  public async run(): Promise<readonly CleanupIssue[]> {
    if (this.completed) return [];
    this.completed = true;
    const issues: CleanupIssue[] = [];
    for (const registration of [...this.registrations].reverse()) {
      try {
        await registration.cleanup();
      } catch (error) {
        const issue: CleanupIssue = {
          category: 'CLEANUP', resource: registration.resource,
          message: error instanceof Error ? error.message : 'Error desconegut de cleanup.'
        };
        issues.push(issue);
        this.reporter.report(issue);
      }
    }
    return issues;
  }
}
