# Contracte territorial PETILOC 1.0

## Finalitat

`PETILOC_TERRITORIAL` versió `1.0` és el format XLSX normalitzat, independent del país, que alimenta el pipeline territorial existent. No substitueix els mappings de fonts externes: el detector l'activa únicament quan troba `Manifest`; la resta de llibres continuen pel mapping versionat.

## Fulls i capçaleres

- `Manifest` — obligatori: `key`, `value`. Claus: `contract_id`, `contract_version`, `country_iso2`, `import_mode`, `dataset_version`, `territorial_date`, `primary_source_key`, `field_scope` i `coverage`.
- `Fonts` — obligatori: `source_key`, `organisation`, `dataset`, `dataset_version`, `territorial_date`, `url`, `license`, `license_url`, `attribution`.
- `Tipus` — obligatori en `FULL`: `type_code`, `name`, `display_order`, `is_selectable_locality`.
- `Unitats` — obligatori: `unit_key`, `identity_scheme`, `identity_value`, `type_code`, `parent_unit_key`, `primary_name`, `primary_locale`, `is_active`, `structure_source_key`, `latitude`, `longitude`, `coordinate_source_key`.
- `Noms` — opcional: `unit_key`, `name`, `locale`, `kind`, `is_primary`, `source_key`, `action`.
- `Codis` — opcional: `unit_key`, `scheme`, `value`, `is_primary`, `valid_from`, `valid_to`, `source_key`, `action`.
- `Atributs` — reservat; qualsevol dada és un error en 1.0.

Els noms de full i les capçaleres són contractuals. Fulls o columnes desconeguts, claus duplicades, referències inexistents, cicles, fonts no declarades, dates/tipus invàlids i versions incompatibles bloquegen la preparació.

## Identitat, jerarquia i profunditat

`unit_key` és la clau estable dins del llibre. La coincidència amb el catàleg es fa pel parell exacte `identity_scheme` + `identity_value`; no s'utilitzen noms ni coincidències de sufix. `parent_unit_key` es resol amb independència de l'ordre de les files, admet arrels, nivells opcionals i profunditat variable.

Els àlies d'esquema han de ser explícits, deterministes i acotats per país i forma del valor. Per a Espanya, el contracte accepta `INE` amb un codi de cinc dígits com a àlies de `es:ine:municipality`. Qualsevol altre valor `INE`, país o esquema desconegut es conserva sense reinterpretar i falla de manera segura si no existeix al catàleg.

## FULL i PATCH

`FULL` representa una fotografia completa: pot proposar altes, canvis i inactivacions sota els gates ja existents. `Tipus` és obligatori.

`PATCH` només opera sobre `field_scope`, no crea ni inactiva unitats i bloqueja identitats absents o ambigües. La primera operació publicable implementada és `PATCH coordinates`; altres scopes es detecten i es bloquegen amb `PATCH_SCOPE_NOT_IMPLEMENTED` fins que disposin d'aplicació parcial completa.

Semàntica de cel·les:

- buit: `KEEP`;
- valor complet: `SET`;
- `@CLEAR` a latitud i longitud: `CLEAR`;
- una sola coordenada, `(0,0)` o valors fora de rang: error.

Els overrides manuals de coordenades es preserven i es mostren com a conflicte. El ChangeSet persistent conserva l'operació de camp per poder revisar i revertir sense reinterpretar el llibre.

## Procedència i publicació

Cada font té un `source_key` estable dins del país. Una importació pot relacionar fonts de referència i fonts contribuïdores. Preparar i revisar és possible amb una font `Pending`; publicar exigeix que totes les fonts que aporten camps siguin actives i `Approved`.

La procedència queda separada per estructura, noms/codis i coordenades. El contracte no incorpora atributs genèrics persistents.

## Preparació externa de nous països

1. Conservar el fitxer oficial original sense modificar-lo.
2. Inventariar identificadors oficials, nivells opcionals, relacions pare-fill, idiomes, coordenades i llicència.
3. Assignar `source_key` estables i donar d'alta les fonts com a `Pending`.
4. Generar un llibre nou amb el contracte 1.0, claus estables i relacions demostrables.
5. Validar-lo a l'assistent; resoldre qualsevol identitat o jerarquia ambigua amb revisió humana.
6. Aprovar documentalment cada font contribuïdora abans de publicar.

No s'han d'inferir pares, coordenades ni codis a partir de noms o patrons no documentats.
