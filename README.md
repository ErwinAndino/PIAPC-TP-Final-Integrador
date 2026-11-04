# PIAPC-TP-Final-Integrador

Trabajo final para la cátedra de Programación de Inteligencia Artificial y Patrones de Comportamiento.

Prototipo **Defend & Collect**: tower-defense ligero 2D top-down sobre grid, hecho en Unity (C#).
Aliados recolectan recursos, enemigos los atacan y el jugador coloca muros/torres en tiempo real para bloquear caminos y proteger a los aliados.

## Presentación

- [docs/presentacion.html](docs/presentacion.html): diapositivas del trabajo. Se abre en el navegador; flechas para navegar.

## Documentación

- [GDD.md](GDD.md): diseño general del prototipo.
- [specs/README.md](specs/README.md): índice de specs, dependencias y estado.
- [AGENTS.md](AGENTS.md): entorno, comandos y convenciones de trabajo.

## Requisitos

- Unity **6000.4.9f1** (Unity 6).
- Render pipeline: **2D Built-in**.
- Grid del prototipo: **30x30**.

## Cómo abrir el proyecto

1. Unity Hub → **Add project from disk**.
2. Seleccionar la raíz de este repositorio.
3. Abrir con Unity `6000.4.9f1`.
4. Abrir la escena `Assets/Scenes/Main.unity`.

## Estructura

```
Assets/
  Scenes/        Escenas jugables (Main.unity)
  Scripts/       Código del juego (assembly DefendAndCollect)
    Core/ Grid/ Pathfinding/ Navigation/ FSM/
    Perception/ Agents/ Buildings/ Scenarios/ Waves/ UI/
  Prefabs/       Prefabs reutilizables
  Art/           Recursos visuales
  Tests/         EditMode y PlayMode
Packages/        Dependencias de Unity
ProjectSettings/ Configuración del proyecto
specs/           Especificaciones de implementación
```
