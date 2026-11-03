# AGENTS.md

Guía para agentes y colaboradores que trabajen en este repositorio.

## Proyecto

**Defend & Collect**: prototipo tower-defense 2D top-down sobre grid, hecho en Unity (C#).
Aliados recolectan recursos, enemigos los atacan y el jugador coloca muros/torres en tiempo real.
Diseño en [GDD.md](GDD.md); tareas en [specs/](specs/).

## Entorno

- Unity **6000.4.9f1** (Unity 6).
- Render pipeline: **2D Built-in** (no agregar URP).
- Grid del prototipo: **30x30**.
- El proyecto Unity vive en la raíz del repo (`Assets/`, `Packages/`, `ProjectSettings/`).

## Comandos

Abrir el proyecto: Unity Hub → *Add project from disk* → seleccionar la raíz del repo.

Importar/validar en batchmode (sin abrir la UI):

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe" `
  -batchmode -nographics -quit `
  -projectPath "<ruta-al-repo>" `
  -logFile "<ruta-al-log>"
```

Tests EditMode:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.9f1\Editor\Unity.exe" `
  -batchmode -nographics `
  -projectPath "<ruta-al-repo>" `
  -runTests -testPlatform EditMode `
  -testResults "<ruta-al-results.xml>" `
  -logFile "<ruta-al-log>"
```

Para PlayMode, cambiar `-testPlatform PlayMode`.
No agregar `-quit` a los comandos de tests: Unity cierra antes de ejecutarlos.

## Convenciones

- C#: 4 espacios, UTF-8, LF.
- Scripts en `Assets/Scripts/<área>` dentro del assembly `DefendAndCollect`.
- Tests en `Assets/Tests/EditMode` y `Assets/Tests/PlayMode`.
- Versionar siempre los `.meta`; nunca versionar `Library/`, `Temp/`, `Logs/`, `UserSettings/`, `.csproj` ni `.sln`.
- No usar trayectorias fijas: las rutas deben recalcularse cuando cambia el grid (ver specs 002, 003 y 008).

## Orden de implementación

1. `spec-001` Grid básico
2. `spec-002` Pathfinding base (BFS y A*)
3. `spec-003` Navegación con costes de terreno
4. `spec-004` FSM para NPCs
5. `spec-005` Percepción y objetivos
6. `spec-006` Aliados: recolección y transporte
7. `spec-007` Enemigos: búsqueda y ataque
8. `spec-008` Construcciones dinámicas
9. `spec-009` Escenarios con plantillas
10. `spec-010` Oleadas, victoria y derrota

El estado de cada spec se mantiene en [specs/README.md](specs/README.md).

## Definición de terminado (por spec)

- Cumple el “Comportamiento esperado”.
- Respeta el “Fuera de alcance”.
- Compila sin errores en batchmode.
- Tiene tests EditMode/PlayMode cuando aplique.
- Se puede comprobar jugando según la sección “Cómo comprobarla jugando”.
