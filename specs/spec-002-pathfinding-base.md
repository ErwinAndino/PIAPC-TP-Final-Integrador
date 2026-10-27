# Spec 002 - Pathfinding Base (BFS y A*)

## Objetivo
Implementar BFS y A* sobre el grid del spec 001. Deben poder seleccionarse.

## Comportamiento esperado
- Interfaz común para pathfinding (GetPath(start, end) retorna lista de tiles).
- Implementación BFS.
- Implementación A* con heurística Manhattan (o admisible simple).
- Mecanismo para seleccionar algoritmo activo (desde código/UI simple).
- Funciona sobre grid 10x10 con obstáculos.

## Fuera de alcance
- Costes de terreno (ver spec 003)
- Recalculo automático por cambios dinámicos (ver spec 008) - solo debe funcionar con grid actual

## Cómo comprobarla jugando
1. Colocar inicio/fin en tiles transitables.
2. Seleccionar BFS → mostrar ruta.
3. Seleccionar A* → mostrar ruta.
4. Cambiar obstáculos → ruta cambia según algoritmo seleccionado.
5. Ver que ambas funcionan y son seleccionables.
