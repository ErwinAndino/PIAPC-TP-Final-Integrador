# Spec 010 - Oleadas, Victoria y Derrota

## Objetivo
Implementar sistema de oleadas, condición victoria/derrota.

## Comportamiento esperado
- Oleadas: empiezan/escalonan enemigos (simple).
- Victoria: sobrevivir X oleadas (ej. 2-3).
- Derrota: todos los aliados mueren.
- UI simple para estado (oleada actual, aliados vivos).
- Integrado con resto del sistema.

## Fuera de alcance
- Muchas oleadas
- Sistema complejo de recompensas

## Cómo comprobarla jugando
1. Iniciar partida → oleada 1.
2. Al matar todos enemigos oleada → siguiente oleada o victoria.
3. Si mueren todos aliados → derrota.
4. Al completar X oleadas → victoria.
5. Estado se muestra claramente.
