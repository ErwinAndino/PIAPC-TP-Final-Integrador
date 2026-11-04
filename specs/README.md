# Specs - Defend & Collect

Índice de las specs del prototipo y su estado de implementación.
Cada spec es un archivo independiente en este directorio; el orden sugerido respeta las dependencias técnicas.

## Estado

| # | Spec | Tema | Depende de | Estado |
|---|------|------|------------|--------|
| 001 | [Grid básico](spec-001-grid-basico.md) | Grid 30x30 y tiles transitables | — | Hecha |
| 002 | [Pathfinding base](spec-002-pathfinding-base.md) | BFS y A* seleccionables | 001 | Hecha |
| 003 | [Navegación con costes](spec-003-navegacion-costes.md) | Costes de terreno seleccionables | 001, 002 | Hecha |
| 004 | [FSM para NPCs](spec-004-fsm-npcs.md) | Máquina de estados base | 002 | Hecha |
| 005 | [Percepción y objetivos](spec-005-percepcion-objetivos.md) | Detección por radio y priorización | 004 | Hecha |
| 006 | [Aliados: recolección](spec-006-aliados-recoleccion.md) | Buscar, recolectar y transportar | 003, 004, 005 | Hecha |
| 007 | [Enemigos: ataque](spec-007-enemigos-ataque.md) | Buscar, perseguir y atacar | 003, 004, 005 | Hecha |
| 008 | [Construcciones dinámicas](spec-008-construcciones-dinamicas.md) | Colocar muros/torres y recalcular rutas | 002, 004 | Hecha |
| 009 | [Escenarios con plantillas](spec-009-escenarios-plantillas.md) | 3 layouts jugables | 001, 003 | Hecha |
| 010 | [Oleadas, victoria y derrota](spec-010-oleadas-victoria-derrota.md) | Flujo de partida | 006, 007, 008, 009 | Hecha |

Estados posibles: `Pendiente`, `En curso`, `Bloqueada`, `Hecha`.

## Cómo trabajar una spec

1. Leer `Objetivo`, `Comportamiento esperado` y `Fuera de alcance`.
2. Implementar solo lo que entra en alcance.
3. Agregar tests en `Assets/Tests/EditMode` o `Assets/Tests/PlayMode`.
4. Validar con Unity en batchmode (ver [AGENTS.md](../AGENTS.md)).
5. Actualizar el estado en esta tabla y verificar la sección “Cómo comprobarla jugando”.

## Definición de terminado

- Cumple el “Comportamiento esperado”.
- No invade el “Fuera de alcance”.
- Compila sin errores.
- Tiene tests cuando aplique.
- Se puede comprobar jugando.
