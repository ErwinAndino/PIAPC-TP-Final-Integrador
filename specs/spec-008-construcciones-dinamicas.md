# Spec 008 - Construcciones Dinámicas en Tiempo Real

## Objetivo
Permitir al jugador colocar muros/torres durante la partida. Esto modifica el grid (obstáculos) y obliga a recalcular rutas.

## Comportamiento esperado
- Modo construcción: colocar muro o torre en tile válido (no ocupado).
- Colocar agrega obstáculo/cambio al grid.
- NPCs recalculan ruta cuando grid cambia (o al detectar camino bloqueado).
- No se permite construir sobre entidades importantes (aliados/enemigos/recursos/depósito) - evitar bloquear completamente si posible.
- Funciona en tiempo real.

## Fuera de alcance
- Menú complejo
- Costos de recursos para construir

## Cómo comprobarla jugando
1. Durante partida, colocar muro frente a aliado/enemigo.
2. Aliado/enemigo recalcula ruta y toma camino alternativo.
3. Colocar bloqueo que fuerce ruta larga → cambia comportamiento.
4. Ver que rutas se adaptan (no siguen trayectoria fija).
5. Se puede colocar muros/torres en tiempo real.
