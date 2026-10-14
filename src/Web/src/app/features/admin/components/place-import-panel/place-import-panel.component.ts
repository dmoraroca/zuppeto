import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TerritorialLocationSelectorComponent, TerritorialLocationSelection } from '../../../../shared/components/territorial-location-selector/territorial-location-selector.component';
import { PlaceImportContext, PlaceImportDetails, PlaceImportRun, PlaceImportService } from '../../services/place-import.service';

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
  protected readonly error=signal(''); protected readonly success=signal('');
  protected countryId=''; protected territorialUnitId=''; protected country=''; protected locality='';
  protected accessMode='OSM_GEOFABRIK_EXTRACT'; protected restaurant=true; protected cafe=true; protected reason='';
  private pollTimer:ReturnType<typeof setTimeout>|null=null;
  async ngOnInit():Promise<void>{await this.reload();}
  ngOnDestroy():void{if(this.pollTimer)clearTimeout(this.pollTimer);}
  protected selectionChanged(value:TerritorialLocationSelection):void{this.countryId=value.countryId;this.territorialUnitId=value.territorialUnitId;this.country=value.country;this.locality=value.locality;}
  protected async prepare():Promise<void>{
    const categories=[this.restaurant?'restaurant':'',this.cafe?'cafe':''].filter(Boolean);
    if(!this.countryId||!this.territorialUnitId){this.error.set('Selecciona un país i un municipi oficial.');return;}
    if(!categories.length){this.error.set('Selecciona almenys una categoria.');return;}
    await this.run(async()=>{const run=await this.api.prepare({countryId:this.countryId,territorialUnitId:this.territorialUnitId,categories,accessMode:this.accessMode,simulationOnly:true});this.success.set('Importació preparada. El worker iniciarà la simulació.');await this.reload(run.id);},'No s’ha pogut preparar la importació.');
  }
  protected async select(run:PlaceImportRun):Promise<void>{await this.run(async()=>{this.details.set(await this.api.get(run.id));this.schedulePoll();},'No s’ha pogut consultar la importació.');}
  protected async approve():Promise<void>{const item=this.details();if(!item||!this.reason.trim()){this.error.set('Indica el motiu de l’aprovació.');return;}await this.run(async()=>{await this.api.approve(item.run.id,this.reason.trim());this.success.set('Simulació aprovada per revisió humana.');await this.reload(item.run.id);},'No s’ha pogut aprovar la simulació.');}
  protected async publish():Promise<void>{const item=this.details();if(!item||!this.reason.trim()){this.error.set('Indica el motiu de la publicació simulada.');return;}await this.run(async()=>{await this.api.simulatePublication(item.run.id,this.reason.trim());this.success.set('Publicació simulada completada. No s’ha creat cap Place real.');await this.reload(item.run.id);},'No s’ha pogut completar la publicació simulada.');}
  protected async cancel():Promise<void>{const item=this.details();if(!item)return;await this.run(async()=>{await this.api.cancel(item.run.id);this.success.set('Importació cancel·lada.');await this.reload(item.run.id);},'No s’ha pogut cancel·lar la importació.');}
  protected statusLabel(value:string):string{return({Prepared:'Preparada',Running:'En execució',Validated:'Validada',PendingReview:'Pendent de revisió',Approved:'Aprovada',Published:'Publicada (simulació)',Failed:'Fallida',Cancelled:'Cancel·lada'} as Record<string,string>)[value]??value;}
  protected categoryLabel(value:string):string{return value==='cafe'?'Cafeteria':value==='restaurant'?'Restaurant':value;}
  private async reload(selectedId?:string):Promise<void>{await this.run(async()=>{const [context,runs]=await Promise.all([this.api.context(),this.api.list()]);this.context.set(context);this.runs.set(runs);if(!context.sources.some(source=>source.key===this.accessMode)&&context.sources.length)this.accessMode=context.sources[0].key;const id=selectedId??this.details()?.run.id;if(id){this.details.set(await this.api.get(id));this.schedulePoll();}},'No s’ha pogut carregar el gestor d’importacions.');}
  private schedulePoll():void{if(this.pollTimer)clearTimeout(this.pollTimer);const status=this.details()?.run.status;if(status!=='Prepared'&&status!=='Running')return;this.pollTimer=setTimeout(()=>void this.reload(this.details()?.run.id),1200);}
  private async run(action:()=>Promise<void>,fallback:string):Promise<void>{this.loading.set(true);this.error.set('');try{await action();}catch(error:unknown){const response=error as {error?:{message?:string}};this.error.set(response.error?.message||fallback);}finally{this.loading.set(false);this.cdr.markForCheck();}}
}
