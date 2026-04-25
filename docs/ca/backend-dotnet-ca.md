# Backend .NET (CA)

## Objectiu

Aquest document tanca el punt de Fase III dedicat a `backend .NET`.

## Resultat

El backend ja queda estructurat en:

- `Domain`
- `Application`
- `Infrastructure`
- `Api`

## Peces creades a Application

- `DependencyInjection.cs`
- `IPlaceApplicationService` / `PlaceApplicationService`
- `IFavoriteListApplicationService` / `FavoriteListApplicationService`
- `IUserApplicationService` / `UserApplicationService`
- `IPlaceReviewApplicationService` / `PlaceReviewApplicationService`
- DTOs i contractes de cada àrea

## UML del backend

<pre style="background:#020617; color:#e5eef7; border:1px solid #1e293b; border-radius:16px; padding:20px; margin:16px 0; overflow:auto; line-height:1.65;"><code><span style="color:#5eead4; font-weight:700;">flowchart LR</span>
  <span style="color:#93c5fd;">API[Api]</span> --&gt; <span style="color:#c4b5fd;">APP[Application]</span>
  <span style="color:#c4b5fd;">APP</span> --&gt; <span style="color:#86efac;">DOM[Domain]</span>
  <span style="color:#c4b5fd;">APP</span> --&gt; <span style="color:#fcd34d;">REP[Repository Contracts]</span>
  <span style="color:#fcd34d;">REP</span> --&gt; <span style="color:#f9a8d4;">INF[Infrastructure]</span>
  <span style="color:#f9a8d4;">INF</span> --&gt; <span style="color:#67e8f9;">DB[(PostgreSQL)]</span></code></pre>

Resum del diagrama:

- `Application` és la capa que articula els casos d'ús
- el domini continua aïllat de la persistència
- `Infrastructure` implementa els contractes necessaris
- `Api` queda preparada per exposar els casos d'ús

## Verificacio

Validat amb:

- `dotnet build __Zuppeto_sln__`

Resultat:

- compilacio correcta del backend complet

## Punt següent

El següent punt actiu passa a ser:

- `API per places, favorites, users i reviews`
