import { resolve } from 'node:path';
import { auditCiArtifacts } from '../infrastructure/ci/ci-artifact-auditor.js';

const path = process.argv.slice(2).find((arg) => arg.startsWith('--path='))?.slice('--path='.length);
if (!path) throw new Error('Falta --path per auditar els artefactes CI.');

void auditCiArtifacts(resolve(path)).then((result) => {
  console.log(`Artefactes CI segurs: ${result.files} fitxers i ${result.archives} arxius comprimits.`);
}).catch((error) => {
  console.error(error instanceof Error ? error.message : 'Error desconegut auditant artefactes CI.');
  process.exitCode = 1;
});
