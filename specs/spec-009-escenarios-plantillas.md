# Spec 009 - Escenarios con Plantillas

## Objetivo
Crear 3 plantillas predefinidas jugables que cambian layout, terreno, posiciones.

## Comportamiento esperado
- 3 plantillas diferentes (distribución muros/terreno/recursos/spawn).
- Al cargar plantilla, se genera grid correctamente.
- Todas jugables y permiten cumplir objetivo (sobrevivir oleadas).
- Compatible con costes de terreno y con algoritmos seleccionables.
- Pueden usarse como base antes de construcciones dinámicas.

## Fuera de alcance
- Generación procedural aleatoria compleja
- Guardado de escenarios

## Cómo comprobarla jugando
1. Seleccionar/cargar plantilla 1 → jugar y ver que es jugable.
2. Plantilla 2 → jugable.
3. Plantilla 3 → jugable.
4. En cada una, rutas funcionan con BFS/A* y con costes seleccionables.
5. Todas permiten avanzar hacia objetivo.
