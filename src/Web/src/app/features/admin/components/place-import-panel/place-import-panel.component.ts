import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TerritorialLocationSelectorComponent, TerritorialLocationSelection } from '../../../../shared/components/territorial-location-selector/territorial-location-selector.component';
import { PlaceCatalogGovernance, PlaceImportContext, PlaceImportDetails, PlaceImportRun, PlaceImportService } from '../../services/place-import.service';

@Component({
  selector: 'app-place-import-panel',
  imports: [DatePipe, FormsModule, TerritorialLocationSelectorComponent],
  templateUrl: './place-import-panel.component.html',
  styleUrl: './place-import-panel.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PlaceImportPanelComponent implements OnInit, OnDestroy {
  private readonly api=inject(PlaceImportService); private readonly cdr=inject(ChangeDetectorRef);
  protected readonly context=signal<PlaceImportContext|null>(null); protected readonly runs=signal<PlaceImportRun[]>([]);
  protected readonly details=signal<PlaceImportDetails|null>(null); protected readonly loading=signal(false);
  protected readonly governance=signal<PlaceCatalogGovernance|null>(null);
  protected readonly error=signal(''); protected readonly success=signal('');
  protected countryId=''; protected territorialUnitId=''; protected country=''; protected locality='';
  protected accessMode='OSM_GEOFABRIK_EXTRACT'; protected readonly selectedCategories=new Set<string>(); protected reason='';
  protected sourceKey=''; protected sourceOrganisation=''; protected sourceDataset=''; protected sourceVersion=''; protected sourceCoverage='';
  protected sourceUrl=''; protected sourceAttributionUrl=''; protected sourceLicense=''; protected sourceLicenseUrl=''; protected sourceAttribution=''; protected sourceReuseConditions='';
  protected sourceCommercialUse=false; protected sourcePermanentStorage=false;
  private pollTimer:ReturnType<typeof setTimeout>|null=null;
  async ngOnInit():Promise<void>{await this.reload();}
  ngOnDestroy():void{if(this.pollTimer)clearTimeout(this.pollTimer);}
  protected selectionChanged(value:TerritorialLocationSelection):void{this.countryId=value.countryId;this.territorialUnitId=value.territorialUnitId;this.country=value.country;this.locality=value.locality;}
  protected async prepare():Promise<void>{
    const categories=[...this.selectedCategories];
    if(!this.countryId||!this.territorialUnitId){this.error.set('Selecciona un país i un municipi oficial.');return;}
    if(!categories.length){this.error.set('Selecciona almenys una categoria.');return;}
    await this.run(async()=>{const run=await this.api.prepare({countryId:this.countryId,territorialUnitId:this.territorialUnitId,categories,accessMode:this.accessMode,simulationOnly:true});this.success.set('Importació de Fase V preparada en simulació.');await this.reload(run.id);},'No s’ha pogut preparar la importació.');
  }
  protected async select(run:PlaceImportRun):Promise<void>{await this.run(async()=>{this.details.set(await this.api.get(run.id));this.schedulePoll();},'No s’ha pogut consultar la importació.');}
  protected async approve():Promise<void>{const item=this.details();if(!item||!this.reason.trim()){this.error.set('Indica el motiu de l’aprovació.');return;}await this.run(async()=>{await this.api.approve(item.run.id,this.reason.trim());this.success.set('Importació aprovada per revisió humana.');await this.reload(item.run.id);},'No s’ha pogut aprovar la importació.');}
  protected async publish():Promise<void>{const item=this.details();if(!item||!this.reason.trim()){this.error.set('Indica el motiu de la publicació.');return;}await this.run(async()=>{item.run.simulationOnly?await this.api.simulatePublication(item.run.id,this.reason.trim()):await this.api.publish(item.run.id,this.reason.trim());this.success.set(item.run.simulationOnly?'Publicació simulada completada. No s’ha creat cap Place real.':'Pilot d’Arenys publicat al catàleg.');await this.reload(item.run.id);},'No s’ha pogut completar la publicació.');}
  protected async cancel():Promise<void>{const item=this.details();if(!item)return;await this.run(async()=>{await this.api.cancel(item.run.id);this.success.set('Importació cancel·lada.');await this.reload(item.run.id);},'No s’ha pogut cancel·lar la importació.');}
  protected statusLabel(value:string):string{return({Prepared:'Preparada',Running:'En execució',Validated:'Validada',PendingReview:'Pendent de revisió',Approved:'Aprovada',Published:'Publicada',Failed:'Fallida',Cancelled:'Cancel·lada'} as Record<string,string>)[value]??value;}
  protected categoryLabel(value:string):string{return value==='cafe'?'Cafeteria':value==='restaurant'?'Restaurant':value;}
  protected toggleCategory(key:string,checked:boolean):void{checked?this.selectedCategories.add(key):this.selectedCategories.delete(key);}
  protected async resolveMatch(id:string,outcome:string,placeId:string|null):Promise<void>{if(!this.reason.trim()){this.error.set('Indica el motiu de la decisió.');return;}await this.run(async()=>{await this.api.resolveMatch(id,outcome,placeId,this.reason.trim());await this.reload();},'No s’ha pogut resoldre el matching.');}
  protected async resolveConflict(id:string,observationId:string):Promise<void>{if(!this.reason.trim()){this.error.set('Indica el motiu de la decisió.');return;}await this.run(async()=>{await this.api.resolveConflict(id,observationId,this.reason.trim());await this.reload();},'No s’ha pogut resoldre el conflicte.');}
  protected async lifecycle(placeId:string,status:string):Promise<void>{if(!this.reason.trim()){this.error.set('Indica el motiu de la decisió.');return;}await this.run(async()=>{await this.api.changeLifecycle(placeId,status,['Revisió ADMIN'],this.reason.trim());await this.reload();},'No s’ha pogut actualitzar l’estat del Place.');}
  protected async registerSource():Promise<void>{
    if(!this.sourceKey.trim()||!this.sourceOrganisation.trim()||!this.sourceDataset.trim()||!this.sourceLicense.trim()){this.error.set('Informa la clau, l’organització, el dataset i la llicència de la font.');return;}
    await this.run(async()=>{await this.api.registerSource({sourceKey:this.sourceKey.trim(),organisation:this.sourceOrganisation.trim(),dataset:this.sourceDataset.trim(),datasetVersion:this.sourceVersion.trim(),coverage:this.sourceCoverage.trim(),url:this.sourceUrl.trim(),attributionUrl:this.sourceAttributionUrl.trim(),licenseIdentifier:this.sourceLicense.trim(),licenseUrl:this.sourceLicenseUrl.trim(),attribution:this.sourceAttribution.trim(),reuseConditions:this.sourceReuseConditions.trim(),commercialUseAllowed:this.sourceCommercialUse,permanentStorageAllowed:this.sourcePermanentStorage});this.success.set('Font registrada com a pendent de revisió.');this.clearSourceForm();await this.reload();},'No s’ha pogut registrar la font.');
  }
  protected async reviewSource(id:string,approved:boolean):Promise<void>{if(!this.reason.trim()){this.error.set('Indica el motiu de la revisió.');return;}await this.run(async()=>{await this.api.reviewSource(id,approved,this.reason.trim());this.success.set(approved?'Font aprovada.':'Font rebutjada.');await this.reload();},'No s’ha pogut revisar la font.');}
  private clearSourceForm():void{this.sourceKey='';this.sourceOrganisation='';this.sourceDataset='';this.sourceVersion='';this.sourceCoverage='';this.sourceUrl='';this.sourceAttributionUrl='';this.sourceLicense='';this.sourceLicenseUrl='';this.sourceAttribution='';this.sourceReuseConditions='';this.sourceCommercialUse=false;this.sourcePermanentStorage=false;}
  private async reload(selectedId?:string):Promise<void>{await this.run(async()=>{const [context,runs,governance]=await Promise.all([this.api.context(),this.api.list(),this.api.governance()]);this.context.set(context);this.runs.set(runs);this.governance.set(governance);if(!this.selectedCategories.size)context.categories.forEach(category=>this.selectedCategories.add(category.key));if(!context.sources.some(source=>source.key===this.accessMode)&&context.sources.length)this.accessMode=context.sources[0].key;const id=selectedId??this.details()?.run.id;if(id){this.details.set(await this.api.get(id));this.schedulePoll();}},'No s’ha pogut carregar el gestor d’importacions.');}
  private schedulePoll():void{if(this.pollTimer)clearTimeout(this.pollTimer);const status=this.details()?.run.status;if(status!=='Prepared'&&status!=='Running')return;this.pollTimer=setTimeout(()=>void this.reload(this.details()?.run.id),1200);}
  private async run(action:()=>Promise<void>,fallback:string):Promise<void>{this.loading.set(true);this.error.set('');try{await action();}catch(error:unknown){const response=error as {error?:{message?:string}};this.error.set(response.error?.message||fallback);}finally{this.loading.set(false);this.cdr.markForCheck();}}
}
