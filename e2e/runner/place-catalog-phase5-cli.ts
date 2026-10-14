import assert from 'node:assert/strict';
import { PilotChromeLauncher } from '../infrastructure/playwright/pilot-chrome-launcher.js';

const webBaseUrl = process.env.E2E_BASE_URL ?? 'http://127.0.0.1:4200';
const apiBaseUrl = process.env.E2E_API_URL ?? 'http://127.0.0.1:5311';
const browserApiPort = process.env.E2E_BROWSER_API_PORT ?? '5211';
const adminEmail = process.env.E2E_ADMIN_EMAIL ?? 'admin@admin.adm';
const adminPassword = process.env.E2E_ADMIN_PASSWORD ?? 'Admin123';
const countryId = required('E2E_COUNTRY_ID');
const territorialUnitId = process.env.E2E_TERRITORIAL_UNIT_ID ?? '8680aa5a-292f-4201-8d52-d6381f438af4';
const externalRequests: string[] = [];

void run().catch((error: unknown) => { console.error(error); process.exitCode = 1; });

async function run(): Promise<void> {
  const browser = await new PilotChromeLauncher().launch(false);
  try {
    const login = await fetch(`${apiBaseUrl}/api/auth/login`, { method:'POST', headers:{'content-type':'application/json'}, body:JSON.stringify({email:adminEmail,password:adminPassword}) });
    assert.equal(login.status, 200, `Login ADMIN: HTTP ${login.status}`);
    const session = await login.json() as { accessToken:string; user:{email:string;displayName?:string;name?:string} };
    session.user.name ??= session.user.displayName ?? session.user.email;
    const context = await browser.newContext({ baseURL:webBaseUrl });
    await context.addInitScript(({key,value}) => localStorage.setItem(key,JSON.stringify(value)), {key:'zuppeto-auth-session',value:session});
    const page = await context.newPage();
    await page.route('**/*', async route => {
      const url = new URL(route.request().url());
      if (['127.0.0.1','localhost'].includes(url.hostname)) {
        if (url.port === browserApiPort) { const target = new URL(apiBaseUrl); url.hostname=target.hostname; url.port=target.port; const response=await route.fetch({url:url.toString()}); await route.fulfill({response}); return; }
        await route.continue(); return;
      }
      if (url.protocol === 'data:' || url.protocol === 'blob:') { await route.continue(); return; }
      externalRequests.push(url.toString()); await route.abort('blockedbyclient');
    });

    await page.goto('/admin/llocs',{waitUntil:'networkidle'});
    await page.getByRole('heading',{name:'Cobertura i governança de Places'}).waitFor();
    await page.getByRole('heading',{name:'Fonts i llicències'}).waitFor();

    const first = await prepare(page,session.accessToken);
    const firstDetails = await waitForReview(page,session.accessToken,first.id);
    assert.equal(firstDetails.run.validCount,1);
    assert.equal(firstDetails.run.duplicateCount,1);
    assert.equal(firstDetails.items[0]?.fields['wikidata.id'],'Q999999999');

    let governance = await getGovernance(page,session.accessToken);
    const match = governance.matches.find(item=>item.externalId==='node/950000001');
    assert.ok(match?.outcome==='ConfirmedMatch'||match?.outcome==='ProbableMatch');
    assert.ok(match?.candidatePlaceId);
    assert.ok(match?.signals.some(signal=>['wikidata.id','name:exact','source+externalId'].includes(signal)));
    const conflict = governance.conflicts.find(item=>item.fieldName==='name'&&item.status==='Open');
    assert.ok(conflict,'S’ha de detectar el conflicte OSM/Wikidata pel nom.');

    await api(page,session.accessToken,'POST',`/api/admin/place-catalog-governance/matches/${match!.id}/resolve`,{outcome:'ConfirmedMatch',placeId:match!.candidatePlaceId,reason:'Confirmació E2E per nom, municipi i distància'},200);
    await api(page,session.accessToken,'POST',`/api/admin/place-catalog-governance/conflicts/${conflict!.id}/resolve`,{winningObservationId:conflict!.leftObservationId,reason:'Resolució E2E amb procedències conservades'},200);
    await api(page,session.accessToken,'POST',`/api/admin/place-catalog-governance/places/${match!.candidatePlaceId}/lifecycle`,{status:'SuspectedClosed',evidence:['Absència en una actualització E2E'],reason:'No equival a un tancament definitiu'},200);

    const second = await prepare(page,session.accessToken);
    const secondDetails = await waitForReview(page,session.accessToken,second.id);
    assert.equal(secondDetails.run.duplicateCount,1);
    governance = await getGovernance(page,session.accessToken);
    assert.equal(governance.matches.filter(item=>item.externalId==='node/950000001').length,1,'Només pot existir una decisió vigent per source record.');
    assert.equal(governance.matches.find(item=>item.externalId==='node/950000001')?.outcome,'ConfirmedMatch');
    assert.equal(governance.lifecycles.find(item=>item.placeId===match!.candidatePlaceId)?.status,'SuspectedClosed');
    assert.ok(governance.sources.some(item=>item.sourceKey==='wikidata'&&item.licenseIdentifier==='CC0-1.0'));

    await page.reload({waitUntil:'networkidle'});
    await page.getByRole('heading',{name:'Estat de vida del catàleg'}).waitFor();
    await page.getByText('SuspectedClosed',{exact:false}).first().waitFor();
    assert.equal(externalRequests.length,0,`Crides externes del navegador: ${externalRequests.join(', ')}`);
    await context.close();
    console.log('PASS Chrome focalitzat Fase V: importació, Wikidata CC0, match explicable, conflicte resolt, tancament sospitós, segona importació idempotent i 0 crides externes del navegador.');
  } finally { await browser.close(); }
}

async function prepare(page:import('@playwright/test').Page,token:string):Promise<ImportRun>{return api(page,token,'POST','/api/admin/place-imports',{countryId,territorialUnitId,categories:['restaurant'],accessMode:'OSM_OVERPASS',simulationOnly:true},202);}
async function waitForReview(page:import('@playwright/test').Page,token:string,id:string):Promise<ImportDetails>{for(let attempt=0;attempt<140;attempt++){const details=await api<ImportDetails>(page,token,'GET',`/api/admin/place-imports/${id}`,undefined,200);if(details.run.status==='PendingReview')return details;assert.notEqual(details.run.status,'Failed',details.run.errorMessage??'Importació fallida');await new Promise(resolve=>setTimeout(resolve,300));}throw new Error('La importació no ha arribat a revisió.');}
async function getGovernance(page:import('@playwright/test').Page,token:string):Promise<Governance>{return api(page,token,'GET','/api/admin/place-catalog-governance/',undefined,200);}

interface ImportRun{readonly id:string;readonly status:string;readonly validCount:number;readonly duplicateCount:number;readonly errorMessage?:string}
interface ImportDetails{readonly run:ImportRun;readonly items:readonly {readonly fields:Record<string,string>}[]}
interface Governance{readonly matches:readonly {readonly id:string;readonly externalId:string;readonly outcome:string;readonly candidatePlaceId:string|null;readonly signals:readonly string[]}[];readonly conflicts:readonly {readonly id:string;readonly fieldName:string;readonly status:string;readonly leftObservationId:string}[];readonly lifecycles:readonly {readonly placeId:string;readonly status:string}[];readonly sources:readonly {readonly sourceKey:string;readonly licenseIdentifier:string}[]}
async function api<T>(page:import('@playwright/test').Page,token:string,method:string,path:string,body:unknown,expected:number):Promise<T>{const browserApiBaseUrl=`http://localhost:${browserApiPort}`;const response=await page.evaluate(async({browserApiBaseUrl,token,method,path,body})=>{const result=await fetch(browserApiBaseUrl+path,{method,headers:{authorization:`Bearer ${token}`,...(body===undefined?{}:{'content-type':'application/json'})},body:body===undefined?undefined:JSON.stringify(body)});return{status:result.status,text:await result.text()};},{browserApiBaseUrl,token,method,path,body});assert.equal(response.status,expected,`${method} ${path}: HTTP ${response.status} ${response.text}`);return(response.text?JSON.parse(response.text):undefined)as T;}
function required(name:string):string{const value=process.env[name];if(!value)throw new Error(`Falta ${name}.`);return value;}
