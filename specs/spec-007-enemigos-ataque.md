# Spec 007 - Enemigos: Búsqueda y Ataque

## Objetivo
Completar comportamiento de enemigos: buscar aliado → perseguir → atacar.

## Comportamiento esperado
- Usa pathfinding para perseguir aliado detectado.
- Prioriza aliados.
- Ataca al alcance (daño simple).
- Aliado puede morir → enemigo busca otro aliado.
- Usa algoritmo de búsqueda seleccionable.

## Fuera de alcance
- Oleadas complejas (spec 010)
- Comportamiento grupal

## Cómo comprobarla jugando
1. Enemigo detecta aliado y lo persigue.
2. Al alcanzar alcance, ataca.
3. Aliado pierde vida/muere.
4. Enemigo busca siguiente aliado si queda alguno.
5. Funciona con BFS/A* seleccionables.
