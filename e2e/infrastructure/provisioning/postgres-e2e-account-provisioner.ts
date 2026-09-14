import { execFileSync } from 'node:child_process';
import { pbkdf2Sync, randomBytes } from 'node:crypto';
import type { E2EAccount } from '../../domain/e2e-role.js';

const iterations = 100_000;
const dedicatedAccounts = new Map([
  ['USER', 'user.e2e@zuppeto.local'],
  ['ADMIN', 'admin.e2e@zuppeto.local'],
  ['DEVELOPER', 'developer.e2e@zuppeto.local'],
  ['VIEWER', 'viewer.e2e@zuppeto.local']
] as const);

export class PostgresE2EAccountProvisioner {
  public constructor(
    private readonly repositoryRoot: string,
    private readonly executeSql: (sql: string) => string = (sql) => execFileSync(
      'docker', ['compose', 'exec', '-T', 'db', 'sh', '-lc', 'psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -At -v ON_ERROR_STOP=1'],
      { cwd: repositoryRoot, encoding: 'utf8', input: sql }
    )
  ) {}

  public provisionAdmin(): E2EAccount {
    const account = this.account('ADMIN', 'admin.e2e@zuppeto.local');
    const count = this.scalar("select count(*) from users where email='admin.e2e@zuppeto.local';");
    if (count !== '0') throw new Error('ADMIN E2E ja existeix però no està configurat localment; no se’n canviarà la credencial implícitament.');
    this.execute(`insert into users (email,password_hash,role,display_name,city,country,comments,avatar_url,privacy_accepted,privacy_accepted_at_utc) values ('admin.e2e@zuppeto.local','${hash(account.password)}','Admin','Admin E2E','Barcelona','Espanya','',null,true,current_timestamp);`);
    return account;
  }

  public rotateViewer(): E2EAccount {
    const account = this.account('VIEWER', 'viewer.e2e@zuppeto.local');
    const count = this.scalar("select count(*) from users where email='viewer.e2e@zuppeto.local' and upper(role)='VIEWER';");
    if (count !== '1') throw new Error('No hi ha exactament un compte VIEWER E2E dedicat per configurar.');
    this.execute(`update users set password_hash='${hash(account.password)}' where email='viewer.e2e@zuppeto.local' and upper(role)='VIEWER';`);
    return account;
  }

  public configure(account: E2EAccount): void {
    const expectedEmail = dedicatedAccounts.get(account.role);
    if (account.email !== expectedEmail) throw new Error(`El compte CI ${account.role} no és el compte E2E dedicat esperat.`);
    if (account.password.length < 16) throw new Error(`La credencial CI ${account.role} és massa curta.`);
    const role = account.role[0] + account.role.slice(1).toLowerCase();
    const displayName = `${role} E2E`;
    this.execute(
      `insert into users (email,password_hash,role,display_name,city,country,comments,avatar_url,privacy_accepted,privacy_accepted_at_utc) values (`
      + `'${expectedEmail}','${hash(account.password)}','${role}','${displayName}','Barcelona','Espanya','',null,true,current_timestamp) `
      + `on conflict (email) do update set password_hash=excluded.password_hash, role=excluded.role, display_name=excluded.display_name, `
      + `city=excluded.city, country=excluded.country, comments='', avatar_url=null, privacy_accepted=true, `
      + `privacy_accepted_at_utc=coalesce(users.privacy_accepted_at_utc,current_timestamp);`
    );
  }

  private account(role: 'ADMIN' | 'VIEWER', email: string): E2EAccount {
    return { role, email, password: `${randomBytes(24).toString('base64url')}A1!` };
  }

  private scalar(sql: string): string {
    return this.run(sql).trim();
  }

  private execute(sql: string): void {
    this.run(sql);
  }

  private run(sql: string): string {
    return this.executeSql(sql);
  }
}

function hash(password: string): string {
  const salt = randomBytes(16);
  const key = pbkdf2Sync(password, salt, iterations, 32, 'sha256');
  return `pbkdf2$${iterations}$${salt.toString('base64')}$${key.toString('base64')}`;
}
