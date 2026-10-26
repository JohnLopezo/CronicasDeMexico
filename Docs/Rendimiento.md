# Registro de rendimiento

La referencia es el **PC con i5-6500**, con el nivel de calidad **Bajo**. Se mide en un build de desarrollo (no en el editor), en cada hito.

| Fecha | Hito | PC | Calidad | Resolución | Escena | FPS medio | FPS mín. | Notas |
|---|---|---|---|---|---|---|---|---|
| | | | | | | | | |

## Perfiles de calidad

| Ajuste | Bajo | Medio | Alto |
|---|---|---|---|
| URP Asset | `URP_Bajo` | `URP_Medio` | `URP_Alto` |
| Renderer | `Renderer_Rendimiento` (Forward, sin SSAO) | `Renderer_Rendimiento` | `Renderer_Calidad` (Forward+, SSAO) |
| Render Scale | 0.75 | 1 | 1 |
| Sombra principal | 1024, 1 cascada, 30 m, duras | 2048, 2 cascadas, 40 m, suaves (baja) | 2048, 4 cascadas, 50 m, suaves (alta) |
| Sombras de luces adicionales | No | No | Sí |
| Opaque Texture | No | No | Sí |

El editor usa **Bajo** por defecto (`m_CurrentQuality: 0`); los builds de Windows arrancan en **Medio**. `QualityService` guarda la elección del jugador.
