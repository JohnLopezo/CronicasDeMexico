# Enemigos del arco olmeca

Fuente: `Docs/Historia_Mundo.md` §4.3, §7, §10; GDD `Docs/Diseno.md` §10. **F** = ficción del juego; **D** = documentado; **I** = interpretación.

## Criterios

- **Dos clases de hostilidad** (§4.3): criaturas que atacan por razones naturales (territorio, hambre, miedo) y criaturas **corrompidas** por la Sombra. Se distinguen a simple vista.
- **Señales de corrupción:** grietas luminosas, fragmentos de símbolos, colores ajenos al entorno y animaciones irregulares. Al vencer a un corrompido se puede recuperar un fragmento de memoria menor.
- **No todo se resuelve matando:** algunas criaturas naturales se apaciguan o se evitan, y una misión (`MS_OLM_03`) puede devolver la calma a una zona.
- **Sin "olmecas enemigos" genéricos** (regla 14): el guerrero del pueblo es aliado; si aparecen guerreros corrompidos, son ecos de la memoria y deben justificarse en la historia.
- **IA modular** (GDD §10): Idle → Patrol/Detect → Chase/Reposition → Telegraph → Attack → Recover → Defeated. Ataques importantes con aviso.

## Catálogo (propuesta)

| Enemigo | Clase | Arquetipo (GDD §10) | Dónde | Comportamiento | Tipo |
|---|---|---|---|---|---|
| **Espíritu jaguar corrompido** | Corrompido | Melee rápido | Selva, pisos 1–3 | Acecha y embiste. **Ya existe** como `EspirituJaguar` (`EnemyChaser`) con arte provisional | F (motivo felino: I) |
| Pecarí | Natural | Tanque ligero | Selva | Carga en línea recta si se le molesta; se puede evitar | D (fauna nativa) |
| Cocodrilo de pantano | Natural | Emboscada | Orillas y pantano | Espera en el agua; ataque rápido con aviso; no persigue lejos | D (*Crocodylus moreletii*, nativo; verificar distribución) |
| Murciélagos | Natural | Volador, enjambre | Cuevas y cámaras | Grupos pequeños; molestan más que dañan | D |
| **Eco borrado** | Corrompido | Mágico / distancia | Ruinas y mazmorra | Silueta incompleta que dispara fragmentos de símbolos; se desvanece y reaparece | F |
| **Basalto despierto** | Corrompido | Tanque | Mazmorra, pisos intermedios | Bloque de basalto columnar animado; lento, golpe de área anunciado | F (material: D) |
| **Enredadera de olvido** | Corrompido | Control | Pantano y templo hundido | Raíces que ralentizan o enraízan | F |

## Mini-jefes

| Mini-jefe | Piso (meta de 8) | Idea |
|---|---|---|
| **Jaguar de Piedra** | 4 (en el vertical slice, piso 2) | Criatura ficticia: escultura felina animada por la corrupción. Saltos, zarpazos y un rugido que levanta polvo (GDD §7.1) |
| **Centinela de basalto** (antes "Guardián de la Piedra" en el GDD) | 7 | Se renombra para no confundirse con el jefe final. Guardián menor que custodia el corazón de la mazmorra |

## Jefe final: el Guardián de la Piedra Viva

Figura **original** inspirada en la monumentalidad escultórica olmeca y en las fuerzas de la tierra, la lluvia y la memoria del territorio; **no** es una deidad olmeca documentada (Historia_Mundo §6; GDD §7.1, §10). Protege la Primera Memoria y se enfrenta al protagonista porque la corrupción le hace ver en él una amenaza.

- **Arena:** circular, con piedra, vegetación y agua.
- **Fase 1:** zarpazo, embestida y golpe de tierra.
- **Fase 2 (~60 % de vida):** sube el agua; ondas, rugido y fragmentos.
- **Fase 3 (~25 %):** secuencias combinadas y «Furia de la Primera Piedra», con zonas de impacto anunciadas.
- **Resolución:** se libera de la corrupción, revela un recuerdo y concede el fragmento, la habilidad de tierra y la elección de Bendición (`Docs/Mazmorras/Olmeca.md`).

Las fases vienen del GDD §10; los números son provisionales hasta el balance.
