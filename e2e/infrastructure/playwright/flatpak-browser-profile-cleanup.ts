import { readdir, rm } from 'node:fs/promises';
import { join } from 'node:path';
import type { Browser } from '@playwright/test';

const playwrightProfilePattern = /^playwright_chromiumdev_profile-[A-Za-z0-9]+$/;

export class FlatpakBrowserProfileCleanup {
  public constructor(private readonly temporaryDirectory: string) {}

  public async launch(launchBrowser: () => Promise<Browser>): Promise<Browser> {
    const beforeLaunch = await this.listProfiles();
    let browser: Browser;
    try {
      browser = await launchBrowser();
    } catch (error) {
      await this.removeProfiles(await this.createdSince(beforeLaunch));
      throw error;
    }

    const ownedProfiles = await this.createdSince(beforeLaunch);
    return this.closeWithCleanup(browser, ownedProfiles);
  }

  private closeWithCleanup(browser: Browser, ownedProfiles: readonly string[]): Browser {
    return new Proxy(browser, {
      get: (target, property) => {
        if (property === 'close') {
          return async (...arguments_: Parameters<Browser['close']>): Promise<void> => {
            let closeError: unknown;
            try {
              await target.close(...arguments_);
            } catch (error) {
              closeError = error;
            }
            try {
              await this.removeProfiles(ownedProfiles);
            } catch (cleanupError) {
              throw new AggregateError(
                closeError === undefined ? [cleanupError] : [closeError, cleanupError],
                'CLEANUP: no s\'han pogut eliminar els perfils temporals Flatpak de Playwright.'
              );
            }
            if (closeError !== undefined) throw closeError;
          };
        }
        const value = Reflect.get(target, property, target) as unknown;
        return typeof value === 'function' ? value.bind(target) : value;
      }
    });
  }

  private async createdSince(previousProfiles: ReadonlySet<string>): Promise<string[]> {
    return [...await this.listProfiles()].filter((profile) => !previousProfiles.has(profile));
  }

  private async listProfiles(): Promise<Set<string>> {
    try {
      const entries = await readdir(this.temporaryDirectory, { withFileTypes: true });
      return new Set(entries
        .filter((entry) => entry.isDirectory() && playwrightProfilePattern.test(entry.name))
        .map((entry) => entry.name));
    } catch (error) {
      if ((error as NodeJS.ErrnoException).code === 'ENOENT') return new Set();
      throw error;
    }
  }

  private async removeProfiles(profiles: readonly string[]): Promise<void> {
    for (const profile of profiles) {
      if (!playwrightProfilePattern.test(profile)) {
        throw new Error(`CLEANUP: nom de perfil temporal no segur: ${profile}`);
      }
      await rm(join(this.temporaryDirectory, profile), { recursive: true, force: true });
    }
  }
}

export function flatpakTemporaryDirectory(applicationId: string): string {
  const userId = process.getuid?.();
  const runtimeDirectory = process.env.XDG_RUNTIME_DIR ?? (userId === undefined ? undefined : `/run/user/${userId}`);
  if (runtimeDirectory === undefined) throw new Error('No s\'ha pogut determinar XDG_RUNTIME_DIR per al cleanup Flatpak.');
  return join(runtimeDirectory, '.flatpak', applicationId, 'tmp');
}
