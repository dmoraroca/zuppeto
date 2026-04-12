# Catàleg geogràfic (països / ciutats) — instal·lació al backend

Els fitxers d’aquest directori **no** estan sota `src/Backend/` per limitacions d’escriptura en alguns entorns.

## Pas 1: copiar al repositori

Des de l’arrel del repo (amb permisos d’escriptura a `src/Backend/`):

```bash
chmod +x platform/geographic-backend/install.sh
./platform/geographic-backend/install.sh
```

O manualment: copia cada carpeta `mirror/*` sobre `src/Backend/` mantenint l’estructura (`Domain`, `Application`, `Infrastructure`, `Api`).

## Pas 2: migració i base de dades

```bash
dotnet ef migrations add AddCountriesAndCities --project src/Backend/Infrastructure --startup-project src/Backend/Api
dotnet ef database update --project src/Backend/Infrastructure --startup-project src/Backend/Api
```

## Pas 3: compilar

```bash
dotnet build YepPet.sln
```

Després d’instal·lar el backend, el frontend (`src/Web`) ofereix les pantalles `/admin/paisos` i `/admin/ciutats` que consumeixen `/api/admin/countries` i `/api/admin/cities`.
