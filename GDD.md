# GDD Breve - Defend & Collect

## Idea
Prototipo Tower-defense ligero 2D en grid. Aliados recolectan recursos y los llevan al depósito. Enemigos atacan a los aliados. El jugador defiende colocando construcciones (muros/torres) en tiempo real para bloquear caminos y proteger a los aliados. Objetivo: sobrevivir oleadas.

## Plataforma/Tecnología
- Motor: Unity (C#)
- Vista: 2D Top-Down
- Navegación: Grid-based

## Objetivo
- **Ganar:** Sobrevivir X oleadas (definir número pequeño para prototipo, ej. 2-3 oleadas).
- **Perder:** Todos los aliados mueren.

## Entidades
- **Jugador (1):** Coloca muros y torres durante la partida (en tiempo real). 
- **Aliados (2):** Recolectores. Deben buscar recursos, recogerlos y transportarlos al depósito/base.
- **Enemigos (2-3):** Atacantes. Prioridad: atacar a aliados.
- **Recursos:** Objetos a recolectar por aliados.
- **Depósito/Base:** Punto de entrega de recursos.
- **Construcciones:** Muros (bloquean paso) y Torres (ataque a enemigos/defensa).

## Percepción
- **Rango de visión/detección:** Distancia simple (radio). 
- Aliados detectan recursos cercanos, enemigos detectan aliados cercanos.
- Enemigos priorizan aliados sobre otras cosas.

## Objetivos por NPC
- **Aliados:** Buscar recurso → Recolectar → Transportar al depósito. Si hay amenaza, pueden buscar ruta alternativa/huir según estado.
- **Enemigos:** Buscar aliado → Perseguir → Atacar.

## Comportamiento
- Organizado con **FSM** (Finite State Machine).
- Las rutas **se recalculan** cuando el grid cambia (construcciones colocadas en tiempo real) o al cambiar de escenario.
- No se usa trayectoria fija para patrullaje; búsqueda de objetivos dinámica.

## Búsqueda (seleccionable)
- **BFS** 
- **A***

Ambos sobre grid, seleccionables desde UI/prototipo para observar diferencias.

## Alternativa de navegación (seleccionable)
- **Costes de terreno** (distinto al algoritmo): Grid con dos modos
  1. Costes iguales (todos los tiles transitables con costo 1)
  2. Costes de terreno (ej. normal=1, lento=2). 
- Cambiar modo de navegación es distinto a cambiar algoritmo. Ambos seleccionables.

## Escenarios variables
- **Plantillas predefinidas (3):** Cambian layout - distribución de muros, tipos de terreno, posiciones de recursos/spawn. 
- **Construcciones dinámicas:** El jugador coloca muros/torres en tiempo real → modifican el grid (añaden obstáculos/cambian pasillos) → rutas deben recalcularse. 
- Todos los escenarios/plantillas deben ser jugables y permitir cumplir el objetivo (sobrevivir oleadas).

## Alcance recortado
- Grid 10x10
- 2 Aliados
- 2-3 Enemigos
- Pocos recursos
- 2 tipos de terreno (normal/lento)
- Muros y Torres básicos
- 2-3 oleadas

## Qué observar / demostrar
- Percepción, objetivos, comportamiento con FSM
- Búsqueda seleccionable (BFS/A*)
- Alternativa de navegación seleccionable (costes iguales vs costes de terreno)
- Escenarios variables (plantillas + construcciones dinámicas) que afectan recorridos
- Rutas que se adaptan al cambio (no fijas)
