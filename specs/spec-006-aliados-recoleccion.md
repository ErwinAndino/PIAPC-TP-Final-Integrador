# Spec 006 - Aliados: Recolección y Transporte

## Objetivo
Completar comportamiento de aliados: buscar recurso → recoger → transportar al depósito.

## Comportamiento esperado
- Usa pathfinding (BFS/A* seleccionable) para ir a recurso.
- Recolecta al llegar (contador/simple).
- Transporta al depósito/base.
- Usa navegación con costes según modo seleccionado.
- Se adapta si ruta cambia (recalcula cuando grid cambia).

## Fuera de alcance
- Defensa compleja vs enemigos
- Inventario complejo

## Cómo comprobarla jugando
1. Aliado encuentra recurso y va hacia él.
2. Al llegar, recoge recurso.
3. Va al depósito y entrega.
4. Busca siguiente recurso si hay más.
5. Funciona con BFS y con A* seleccionables.
