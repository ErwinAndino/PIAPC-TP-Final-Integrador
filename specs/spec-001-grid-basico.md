# Spec 001 - Grid Básico

## Objetivo

Crear un grid 30x30 en Unity (C#) que represente el mundo. Debe distinguir tiles transitables y no transitables (obstáculos).

## Comportamiento esperado

- Grid de 30x30 tiles.
- Cada tile tiene tipo: transitable/no transitable.
- Se puede inicializar desde datos (para luego cargar plantillas).
- Visualización básica de grid/obstáculos.

## Fuera de alcance

- Pathfinding (ver spec 002)
- Costes de terreno (ver spec 003)
- Colocación dinámica por jugador (ver spec 008)

## Cómo comprobarla jugando

1. Abrir escena con grid.
2. Ver que hay 30x30 tiles visibles.
3. Algunos tiles son obstáculos (no transitables).
4. Se puede identificar visualmente qué tiles están bloqueados.
