import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { PlaceImportPanelComponent } from './place-import-panel.component';
import { PlaceImportService } from '../../services/place-import.service';
import { TerritorialLocationService } from '../../../../shared/services/territorial-location.service';

describe('PlaceImportPanelComponent', () => {
  let fixture: ComponentFixture<PlaceImportPanelComponent>;
  let api: FakeApi;
  beforeEach(async () => {
    api = new FakeApi();
    await TestBed.configureTestingModule({
      imports: [PlaceImportPanelComponent], providers: [provideHttpClient(), { provide: PlaceImportService, useValue: api },
        { provide: TerritorialLocationService, useValue: { countries: async () => [{id:'es',code:'ES',name:'Espanya'}], localities: async () => [] } }]
    }).compileComponents();
    fixture = TestBed.createComponent(PlaceImportPanelComponent); fixture.detectChanges();
    await fixture.componentInstance.ngOnInit(); fixture.detectChanges();
  });

  it('shows the ODbL source and simulation warning', () => {
    expect(fixture.nativeElement.textContent).toContain('OpenStreetMap');
    expect(fixture.nativeElement.textContent).toContain('Només simulació');
    expect(fixture.nativeElement.textContent).toContain('Derivative/Collective Database');
  });

  it('requires an official territorial selection before preparing', async () => {
    await (fixture.componentInstance as any).prepare(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Selecciona un país i un municipi oficial');
    expect(api.prepared).toBeNull();
  });

  it('prepares both pilot categories with the territorial UUID', async () => {
    (fixture.componentInstance as any).selectionChanged({countryId:'es',country:'Espanya',territorialUnitId:'arenys',locality:'Arenys de Mar'});
    await (fixture.componentInstance as any).prepare(); fixture.detectChanges();
    expect(api.prepared).toEqual(expect.objectContaining({countryId:'es',territorialUnitId:'arenys',categories:['restaurant','cafe'],simulationOnly:true}));
  });
});

class FakeApi {
  prepared:unknown=null;
  async context(){return {sources:[{key:'OSM_GEOFABRIK_EXTRACT',label:'OpenStreetMap / extracte Geofabrik',license:'ODbL-1.0',attribution:'© OpenStreetMap contributors'}],countries:['ES'],categories:[{key:'restaurant',label:'Restaurants'},{key:'cafe',label:'Cafeteries'}],simulationOnly:true};}
  async list(){return [];}
  async get(){throw new Error('unused');}
  async prepare(request:unknown){this.prepared=request;return {id:'run'};}
  async approve(){} async simulatePublication(){} async cancel(){}
}
