#!/usr/bin/env python3
"""Resol municipis Overture per lots amb un índex espacial STRtree."""

from __future__ import annotations

import csv
import unicodedata
from collections import defaultdict
from pathlib import Path

import pyarrow as pa
import pyarrow.compute as pc
import pyarrow.parquet as pq
import shapely
from shapely import Point, STRtree


ROOT = Path("/DATA/imports/petiloc/overture/2026-09-23.1")
MUNICIPALITIES = ROOT / "municipalities-es-de.csv"
COORDINATES = ROOT / "overture-es-de-coordinates.parquet"
OUTPUT = ROOT / "overture-es-de-territories.parquet"
DIVISIONS = [
    ROOT / "es-mainland-balearic-division-area.parquet",
    ROOT / "es-canary-division-area.parquet",
    ROOT / "de-division-area.parquet",
]


def normalized(value: str | None) -> str:
    decomposed = unicodedata.normalize("NFKD", value or "")
    return "".join(character for character in decomposed if not unicodedata.combining(character)).strip().lower()


def municipality_catalog():
    rows = []
    with MUNICIPALITIES.open(newline="", encoding="utf-8") as stream:
        for row in csv.DictReader(stream):
            row["name_key"] = normalized(row["name"])
            row["latitude"] = float(row["latitude"]) if row["latitude"] else None
            row["longitude"] = float(row["longitude"]) if row["longitude"] else None
            rows.append(row)
    units_by_name = defaultdict(set)
    for row in rows:
        units_by_name[(row["country_iso2"], row["name_key"])].add(row["territorial_unit_id"])
    unique = {
        (row["country_iso2"], row["name_key"]): row
        for row in rows
        if len(units_by_name[(row["country_iso2"], row["name_key"])]) == 1
    }
    units = {}
    for row in rows:
        units.setdefault(row["territorial_unit_id"], row)
    return unique, units


def municipal_boundaries(unique_names, municipalities):
    candidates = defaultdict(list)
    divisions = {"ES": [], "DE": []}
    for path in DIVISIONS:
        table = pq.read_table(path, columns=["country", "subtype", "class", "names", "geometry"])
        countries = table["country"].to_pylist()
        subtypes = table["subtype"].to_pylist()
        classes = table["class"].to_pylist()
        names = pc.struct_field(table["names"], "primary").to_pylist()
        geometries = table["geometry"].to_pylist()
        for country, subtype, division_class, name, wkb in zip(countries, subtypes, classes, names, geometries):
            if country not in ("ES", "DE") or subtype != "locality" or division_class != "land" or not wkb:
                continue
            geometry = shapely.from_wkb(wkb)
            divisions[country].append((name, geometry))
            municipality = unique_names.get((country, normalized(name)))
            if municipality is not None:
                candidates[municipality["territorial_unit_id"]].append((municipality, geometry))

    selected = {}
    for options in candidates.values():
        municipality = options[0][0]
        point = Point(municipality["longitude"], municipality["latitude"])
        containing = [geometry for _, geometry in options if shapely.contains(geometry, point)]
        pool = containing or [geometry for _, geometry in options]
        geometry = max(pool, key=shapely.area)
        selected[municipality["territorial_unit_id"]] = (municipality, geometry)

    # Els noms oficials no sempre coincideixen (especialment a Alemanya). Per a
    # les unitats restants, la coordenada oficial ha de caure dins una única
    # àrea local. Es tria l’àrea més gran per evitar barris interns. Si una
    # mateixa àrea resultés assignada a més d’un municipi, es descarta el grup.
    for country in ("ES", "DE"):
        country_divisions = divisions[country]
        geometries = [value[1] for value in country_divisions]
        tree = STRtree(geometries)
        pending = [
            municipality for municipality in municipalities.values()
            if municipality["country_iso2"] == country
            and municipality["territorial_unit_id"] not in selected
            and municipality["latitude"] is not None
            and municipality["longitude"] is not None
        ]
        points = shapely.points(
            [municipality["longitude"] for municipality in pending],
            [municipality["latitude"] for municipality in pending],
        )
        pairs = tree.query(points, predicate="within")
        candidates_by_unit = defaultdict(list)
        for query_index, tree_index in zip(pairs[0], pairs[1]):
            candidates_by_unit[int(query_index)].append(int(tree_index))
        proposals = defaultdict(list)
        for query_index, indexes in candidates_by_unit.items():
            tree_index = max(indexes, key=lambda value: shapely.area(geometries[value]))
            proposals[tree_index].append(query_index)
        for tree_index, query_indexes in proposals.items():
            if len(query_indexes) != 1:
                continue
            municipality = pending[query_indexes[0]]
            selected[municipality["territorial_unit_id"]] = (municipality, geometries[tree_index])

    result = {"ES": [], "DE": []}
    for municipality, geometry in selected.values():
        result[municipality["country_iso2"]].append((municipality, geometry))
    return result


def output_schema():
    return pa.schema([
        ("external_id", pa.string()),
        ("territorial_unit_id", pa.string()),
        ("territorial_name", pa.string()),
        ("official_code", pa.string()),
        ("territory_match_method", pa.string()),
    ])


def resolve(boundaries):
    indexes = {}
    metadata = {}
    areas = {}
    for country, values in boundaries.items():
        geometries = [value[1] for value in values]
        indexes[country] = STRtree(geometries)
        metadata[country] = [value[0] for value in values]
        areas[country] = [shapely.area(geometry) for geometry in geometries]

    writer = pq.ParquetWriter(OUTPUT, output_schema(), compression="zstd")
    try:
        parquet = pq.ParquetFile(COORDINATES)
        for batch in parquet.iter_batches(batch_size=100_000):
            data = batch.to_pydict()
            resolved = [None] * batch.num_rows
            for country in ("ES", "DE"):
                positions = [index for index, value in enumerate(data["country_iso2"]) if value == country]
                if not positions:
                    continue
                points = shapely.points(
                    [data["longitude"][index] for index in positions],
                    [data["latitude"][index] for index in positions],
                )
                pairs = indexes[country].query(points, predicate="within")
                selected = {}
                for query_index, tree_index in zip(pairs[0], pairs[1]):
                    previous = selected.get(int(query_index))
                    if previous is None or areas[country][int(tree_index)] < areas[country][previous]:
                        selected[int(query_index)] = int(tree_index)
                for query_index, tree_index in selected.items():
                    resolved[positions[query_index]] = metadata[country][tree_index]

            rows = {name: [] for name in output_schema().names}
            for index, external_id in enumerate(data["external_id"]):
                municipality = resolved[index]
                method = "SpatialBoundary"
                if municipality is None and data["locality_territorial_unit_id"][index]:
                    municipality = {
                        "territorial_unit_id": data["locality_territorial_unit_id"][index],
                        "name": data["locality_territorial_name"][index],
                        "official_code": data["locality_official_code"][index],
                    }
                    method = "UniqueLocalityName"
                rows["external_id"].append(external_id)
                rows["territorial_unit_id"].append(municipality["territorial_unit_id"] if municipality else None)
                rows["territorial_name"].append(municipality["name"] if municipality else None)
                rows["official_code"].append(municipality["official_code"] if municipality else None)
                rows["territory_match_method"].append(method if municipality else "Unresolved")
            writer.write_table(pa.Table.from_pydict(rows, schema=output_schema()))
    finally:
        writer.close()


if __name__ == "__main__":
    unique_names, municipalities = municipality_catalog()
    resolve(municipal_boundaries(unique_names, municipalities))

