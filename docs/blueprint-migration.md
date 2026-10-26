# Migrar los Blueprints a `/api/v1`

Esta guía lista los cambios que necesita el simulador (`BP_FirstPersonCharacter`, `W_MatchesHistory`, `BPAC_ScoreComponement`) para pasar de la API antigua a la nueva. Los `.uasset` son binarios y se editan en el editor de Unreal.

Sustituye `<API>` por la URL de Azure (o `https://localhost:7069` en desarrollo), y `<SUPABASE>` y `<PUBLISHABLE_KEY>` por los valores de tu proyecto.

## 0. Variables nuevas en `BP_Simulator_GameInstance`

| Variable | Tipo | Uso |
| --- | --- | --- |
| `ApiBaseUrl` | String | Base de la API; no la repitas en cada nodo |
| `AccessToken` | String | Token de Supabase; dura 1 hora |
| `RefreshToken` | String | Para renovar el token sin pedir la contraseña |
| `CurrentMatchId` | String | Reemplaza a `IdMatch`; se replica a los clientes |

En **todas** las peticiones a la API, antes de `ProcessURL`, añade con VaRest `Set Header`:

- `Authorization` = `Bearer ` + `AccessToken`
- `Content-Type` = `application/json`

## 1. Login (nuevo, contra Supabase Auth)

Lo que antes hacía `POST /api/Usuarios` con `{"usuario","rol"}` se divide en dos pasos.

**Registrarse** (una vez):

```
POST <SUPABASE>/auth/v1/signup
apikey: <PUBLISHABLE_KEY>
{ "email": "ana@itm.edu.co", "password": "..." }
```

**Iniciar sesión:**

```
POST <SUPABASE>/auth/v1/token?grant_type=password
apikey: <PUBLISHABLE_KEY>
{ "email": "ana@itm.edu.co", "password": "..." }
→ { "access_token": "...", "refresh_token": "...", "expires_in": 3600, "user": { "id": "..." } }
```

**Crear o actualizar el perfil** (después de cada login):

```
PUT <API>/api/v1/me
{ "displayName": "Ana Gómez" }
→ 201 la primera vez, 200 después
```

**El cliente ya no envía el rol.** Todo usuario nuevo es `Student`, y un admin asigna `Teacher` (ver [supabase.md](supabase.md#4-primer-administrador)).

**Renovar el token:** si una llamada devuelve `401`, renueva y repite la llamada.

```
POST <SUPABASE>/auth/v1/token?grant_type=refresh_token
apikey: <PUBLISHABLE_KEY>
{ "refresh_token": "<RefreshToken>" }
```

> No guardes la contraseña. Si quieres "recordar sesión", guarda solo el `refresh_token` en el `SaveGame`.

## 2. Partidas: de "guardar al final" a un ciclo de vida

| Momento en el juego | Antes | Ahora |
| --- | --- | --- |
| El docente crea la sesión (host) | — | `POST /api/v1/matches` `{ "roomName": "<ServerName>" }` → guarda `id` en `CurrentMatchId` |
| Un estudiante entra a la sesión | `POST /api/UsuarioPartidas` `{ "Usuario", "IdPartida" }` | `POST /api/v1/matches/{CurrentMatchId}/participants` (sin body) |
| "Terminar Partida" | `POST /api/Partidas` `{ NombreSala, NombreDocente, CantidadJugadores, FechaFinal }` | `POST /api/v1/matches/{CurrentMatchId}/end` (sin body) |

- `NombreDocente`, `CantidadJugadores` y `FechaFinal` ya **no se envían**: el servidor los conoce (host, participantes y `endedAt`).
- Crear la sesión requiere rol `Teacher`. Si el host es estudiante, recibe `403`.

## 3. Resultado del escenario (`POST /api/Escenario1` → `POST /api/v1/attempts`)

```json
{
  "scenarioCode": "infusion-pump",
  "matchId": "<CurrentMatchId o null en práctica libre>",
  "selectedMedication": "Dopamina",
  "selectedPathology": "Shock cardiogénico",
  "remainingSeconds": 125,
  "steps": [
    { "stepCode": "correct-area", "earnedPoints": 100 },
    { "stepCode": "dirty-pump", "earnedPoints": 100 }
  ]
}
```

El arreglo `steps` debe traer **los 11 pasos, una vez cada uno**:

| Campo antiguo | `stepCode` |
| --- | --- |
| `PuntajeAreaCorrecta` | `correct-area` |
| `PuntajeBombaSucia` | `dirty-pump` |
| `PuntajeCableDañado` | `damaged-power-cable` |
| `PuntajeCableDesconectado` | `power-cable-disconnected` |
| `PuntajeFuenteDesconectada` | `power-source-disconnected` |
| `PuntajeSticker` | `maintenance-sticker` |
| `PuntajeSetIncorporado` | `infusion-set-installed` |
| `PuntajePurga` | `purge` |
| `PuntajeDocumentoPaciente` | `patient-document` |
| `PuntajeMedicamentoCorrecto` | `correct-medication` |
| `PuntajeConfigurarLineas` | `line-configuration` |
| `TiempoRestante` (`"00:{Mins}:{Sec}"`) | `remainingSeconds` = `Mins * 60 + Sec` (número) |
| `CalificacionTotal` | **eliminado**: lo calcula el servidor (`score`, `maxScore`, `scorePercent`) |

Con VaRest: crea un `VaRestJsonObject` por paso (`SetStringField stepCode`, `SetNumberField earnedPoints`), y luego `SetObjectArrayField("steps", ...)` en el objeto principal.

> **Importante:** los números deben ir como números (`SetNumberField`), no dentro de un texto plantilla como `"CantidadJugadores": "{UsersOnline}"`. La API antigua lo toleraba por casualidad.

Los máximos por paso se consultan con `GET /api/v1/scenarios/infusion-pump`. La pantalla de resultados puede usarlos en lugar de tener los valores fijos en el Blueprint.

## 4. Historial (`W_MatchesHistory`)

| Antes | Ahora |
| --- | --- |
| `GET /api/Escenario1`: descargaba **los resultados de todos los usuarios** y se filtraba en el cliente | `GET /api/v1/me/attempts?page=1&pageSize=20`: solo los del usuario |
| `GET /api/Partidas/{user}` | `GET /api/v1/me/matches` |

La respuesta es paginada: `{ "items": [...], "page": 1, "pageSize": 20, "totalCount": 42 }`. Cada intento trae `steps` con `stepCode`, `earnedPoints` y `maxPoints`.

## 5. Errores

Todas las respuestas de error usan el mismo formato:

```json
{ "status": 400, "title": "One or more validation errors occurred.", "errors": { "steps.missing": ["Missing steps: purge."] } }
```

| Código | Qué hacer en el juego |
| --- | --- |
| `401` | Renovar el token; si vuelve a fallar, mandar al login |
| `403` en `/me/*`, `/attempts` o `/matches/*` | Falta el perfil (`PUT /me`), o el usuario no tiene permiso o no está unido a la sesión |
| `409` | La sesión ya terminó |
| `429` | Demasiadas peticiones; reintentar en unos segundos |
