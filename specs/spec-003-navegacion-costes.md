# Spec 003 - Navegación con Costes de Terreno

## Objetivo
Añadir alternativa de navegación distinta al algoritmo: grid con costes de terreno. Dos modos seleccionables.

## Comportamiento esperado
- Modo 1: Costes iguales (todos tiles transitables costo 1)
- Modo 2: Costes de terreno (ej. normal costo 1, lento costo 2). Al menos 2 tipos de terreno.
- Afecta a A* (y debe integrarse con BFS/A* según implementación). BFS sigue siendo BFS pero modo debe poder aplicarse; lo importante es que **alternativa de navegación != cambiar algoritmo**.
- Modo seleccionable independientemente del algoritmo seleccionado.
- Rutas pueden cambiar al cambiar modo de costes.

## Fuera de alcance
- Más de 2 tipos de terreno
- Terreno con efectos complejos

## Cómo comprobarla jugando
1. Tener mismo mapa con terreno distinto.
2. Seleccionar A* + costes iguales → ver ruta.
3. Seleccionar A* + costes de terreno → ver ruta distinta (si hay camino con menor costo total por terreno).
4. Cambiar solo modo de costes (mantener algoritmo) → rutas cambian.
5. Confirmar que esto es distinto a cambiar BFS<->A*.
