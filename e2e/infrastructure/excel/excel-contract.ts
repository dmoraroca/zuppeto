export const provesHeaders = {
  testCode: 'Codi prova',
  browser: 'Navegador',
  role: 'Rol probat',
  date: 'Data',
  result: 'Resultat',
  observations: 'Observacions',
  correctionDate: 'Data Correcció',
  correctionResult: 'Resultat Correcció',
  completedTasks: 'Tasques Fetes',
  scenarioId: 'Id escenari',
  resultOrigin: 'Origen resultat'
} as const;

export const executionsHeaders = [
  'runId',
  'executionId',
  'Id escenari',
  'Codi prova',
  'Rol',
  'Variant',
  'Navegador',
  'Motor',
  'Versió navegador',
  'Entorn',
  'Commit',
  'Inici',
  'Final',
  'Durada (ms)',
  'Attempt',
  'Resultat tècnic',
  'Errors JavaScript',
  'Errors HTTP/xarxa',
  'Missatge',
  'Evidències/rutes relatives',
  'SyncStatus',
  'Origen Local/CI',
  'Referència revalidació'
] as const;

export const pendingResult = 'PENDENT';
export const manualOrigin = 'MANUAL';
export const e2eOrigin = 'E2E';
