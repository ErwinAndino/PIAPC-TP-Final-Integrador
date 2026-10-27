# Spec 005 - Percepción y Objetivos

## Objetivo
Añadir percepción por rango simple para detectar objetivos.

## Comportamiento esperado
- Rango de detección (radio) configurable.
- Aliados detectan recursos cercanos.
- Enemigos detectan aliados cercanos y los priorizan.
- Al detectar objetivo válido, NPC cambia objetivo/estado acorde.
- Si objetivo desaparece (muere/se recoge), busca nuevo objetivo.

## Fuera de alcance
- Línea de visión compleja (raycast con paredes) - usar distancia/rango simple
- Percepción por sonido

## Cómo comprobarla jugando
1. Enemigo detecta aliado al entrar en rango.
2. Aliado detecta recurso al entrar en rango.
3. Al matar aliado, enemigo busca otro aliado o cambia comportamiento.
4. Al recoger recurso, aliado busca otro recurso o va a depósito.
