# Z-PARK — Sobrevive · Mejora · Récord

Juego de supervivencia zombie en **realidad aumentada** para Android. El jugador escanea el suelo, coloca un campo de batalla de unos 15 m y debe resistir **6 oleadas durante 5 minutos**, disparando con **ATACAR**, protegiéndose con **DEFENDER** y comprando mejoras en la **tienda**. Las partidas se guardan en un **ranking local**.

**Equipo:** Laura Daniela Porras Alvarez · Dayanna Jahaira Huertas Manzano · Yeison Stiven Romero Salinas
**Docente:** Ing. David Fernando Bernal Bolívar
**Fundación Universitaria Juan de Castellanos** — Plataformas de Computación — Tunja, 2026

---

## Tecnologías

| Elemento | Versión / detalle |
|---|---|
| Motor | Unity 6 (6000.6.2f1), Universal Render Pipeline (URP) |
| Realidad aumentada | AR Foundation + Google ARCore XR Plugin |
| Entrada | Input System (nuevo) |
| Plataforma | Android 8.0 o superior, compatible con ARCore |
| Compilación | IL2CPP, ARM64, OpenGLES3 |

Lista de celulares compatibles con ARCore: https://developers.google.com/ar/devices?hl=es-419

> Nota: el proyecto está hecho en **Unity** (motor de videojuegos) y no en Android Studio. Unity genera el APK de Android a partir de este proyecto.

---

## Cómo abrir y compilar el proyecto

1. Instalar **Unity Hub** y el editor **Unity 6000.6.2f1** con el módulo **Android Build Support** (incluye SDK, NDK y OpenJDK).
2. Clonar este repositorio y, en Unity Hub, elegir **Add > Add project from disk** y seleccionar la carpeta.
3. La primera apertura tarda varios minutos: Unity regenera la carpeta `Library` (no se sube al repositorio).
4. Abrir la escena `Assets/Scenes/SampleScene`.
5. En **File > Build Profiles**, elegir **Android** como plataforma activa.
6. Conectar un celular Android con **Depuración USB** activada y pulsar **Build And Run**.

---

## Estructura del proyecto

```
Assets/
  Scripts/            Código del juego (C#)
  Art/Zombies/        Modelos 3D (FBX), texturas, materiales y animaciones de los 3 zombies
  Art/ZPark_Icono.png Ícono de la app
  Prefabs/            Zombie (cápsula de respaldo) y Zombie_Infectado / Corredor / Acechador
  Data/               Datos de cada tipo de zombie (ScriptableObjects)
  Scenes/             SampleScene (única escena del juego)
  Settings/           Configuración de URP y perfiles de compilación
Packages/             Paquetes de Unity usados (AR Foundation, ARCore, Input System…)
ProjectSettings/      Configuración del proyecto (Player, XR, calidad…)
```

### Scripts y su función

| Capa | Script | Responsabilidad |
|---|---|---|
| Interfaz (UI) | `HUD.cs` | Crea toda la interfaz por código: menú, alias, HUD (vida, tiempo, oleada, puntos), botones ATACAR/DEFENDER, tienda, pausa, cómo jugar, ajustes, ranking, pantalla de resultado, flechas de zombies fuera de pantalla, sonidos y efectos |
| Interfaz (UI) | `BotonMantener.cs` | Detecta cuándo un botón se mantiene presionado (ATACAR) |
| Lógica | `GameManager.cs` | Estados del juego (menú, preparando, jugando, victoria, derrota), cronómetro, oleadas, vida, escudo, pausa, compras de la tienda y guardado de la partida |
| Lógica | `Arena.cs` | Detección del suelo con ARCore, colocación de la arena y modo de respaldo si no se detecta el suelo |
| Lógica | `ZombieSpawner.cs` | Aparición de zombies alrededor del jugador, dificultad por oleada y elección del tipo |
| Lógica | `Zombie.cs` | Movimiento hacia el jugador, separación entre zombies, ataque, vida, muerte y animaciones |
| Lógica | `DisparoJugador.cs` | Disparo desde la cámara (raycast) con daño, cadencia y proyectiles mejorables |
| Datos | `ZombieData.cs` | Definición de cada tipo de zombie (vida, velocidad, daño, puntos, escala, modelo) |
| Datos | `Ranking.cs` | Guardado del alias y de las partidas en el celular (PlayerPrefs en formato JSON) |
| Editor | `CrearTiposZombie.cs` | Menú **ZPark > Crear tipos de zombie**: crea los 3 tipos y los asigna |
| Editor | `ConfigurarZombies3D.cs` | Menú **ZPark > Configurar zombies 3D**: prepara modelos, materiales URP, animaciones y prefabs |
| Editor | `DiagnosticarZombies.cs` | Menú **ZPark > Diagnosticar zombies**: revisa la configuración de animación de cada prefab |
| Editor | `PrepararEntrega.cs` | Menú **ZPark > Preparar build de entrega**: nombre, versión, ícono y ajustes de compilación |

---

## Controles

| Control | Acción |
|---|---|
| Tocar la pantalla (al inicio) | Colocar la arena sobre el suelo detectado |
| ATACAR (derecha, mantener) | Disparar hacia la mira |
| DEFENDER (izquierda) | Escudo de 2 s; recarga de 8 s |
| TIENDA | Pausa y permite comprar mejoras |
| Pausa (junto al reloj) | Detener la partida |

**Puntuación final** = zombies eliminados × 10 + segundos sobrevividos × 1 + oleadas completadas × 50

---

## Notas para el equipo

- Reemplazar scripts **desde el Explorador de Windows**, nunca arrastrándolos a Unity, y esperar a que termine de compilar.
- Si al compilar aparecen errores de *"serialization layout"* en el celular, cerrar Unity, borrar la carpeta `Library/Bee` y volver a compilar.
- Si un zombie no se anima, correr **ZPark > Diagnosticar zombies** y revisar que tenga `controller=…_Animator`.
