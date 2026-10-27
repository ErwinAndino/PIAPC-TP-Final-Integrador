# Spec 004 - FSM para NPCs

## Objetivo
Crear FSM base para Aliados y Enemigos con estados mínimos.

## Comportamiento esperado
- FSM genérica o por tipo.
- Aliado: estados mínimos (Idle/BuscarRecurso/Recolectar/TransportarADeposito/AtacarDefender o Huir según necesidad)
- Enemigo: estados mínimos (Idle/BuscarAliado/Perseguir/Atacar)
- Transiciones lógicas entre estados.
- Integración con pathfinding para moverse.

## Fuera de alcance
- Lógica completa de recolección/ataque (specs 006/007)
- Percepción compleja (spec 005)

## Cómo comprobarla jugando
1. NPCs cambian estado visible/lógicamente.
2. Aliado pasa por estados coherentes.
3. Enemigo pasa por estados coherentes.
4. Transiciones ocurren según condiciones básicas.
