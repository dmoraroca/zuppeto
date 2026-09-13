import { mkdir, open, rename, rm, unlink } from 'node:fs/promises';
import { dirname, join } from 'node:path';
import { randomUUID } from 'node:crypto';
import ExcelJS from 'exceljs';

export type WorkbookWrite = (workbook: ExcelJS.Workbook, temporaryPath: string) => Promise<void>;

export class WorkbookWriteLock {
  public constructor(private readonly workbookPath: string) {}

  public async execute<T>(operation: () => Promise<T>): Promise<T> {
    const lockPath = this.workbookPath + '.e2e.lock';
    await mkdir(dirname(this.workbookPath), { recursive: true });
    let handle: Awaited<ReturnType<typeof open>>;
    try {
      handle = await open(lockPath, 'wx');
    } catch {
      throw new Error('Excel workbook is already locked: ' + this.workbookPath);
    }
    try {
      return await operation();
    } finally {
      await handle.close();
      await unlink(lockPath).catch(() => undefined);
    }
  }
}

export class AtomicWorkbookWriter {
  public async write(workbook: ExcelJS.Workbook, workbookPath: string): Promise<void> {
    const temporaryPath = join(dirname(workbookPath), '.' + randomUUID() + '.xlsx.tmp');
    try {
      await workbook.xlsx.writeFile(temporaryPath);
      const validation = new ExcelJS.Workbook();
      await validation.xlsx.readFile(temporaryPath);
      await rename(temporaryPath, workbookPath);
    } catch (error) {
      await rm(temporaryPath, { force: true }).catch(() => undefined);
      throw error;
    }
  }
}
