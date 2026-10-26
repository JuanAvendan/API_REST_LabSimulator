# Supabase

Supabase cumple dos funciones: **Auth**, con los usuarios y contraseñas, y **Postgres**, como base de datos gestionada. Desde Postgres, la API solo usa el esquema `labsim`.

## 1. Auth

1. **Authentication → Sign In / Providers**: activa **Email**. Decide si exiges confirmar el correo; para estudiantes con correo institucional se recomienda.
2. **Project Settings → JWT Keys**: verifica que la clave en uso sea **asimétrica (ECC P-256 o RSA)**. La API valida los tokens con las claves públicas de `https://<ref>.supabase.co/auth/v1/.well-known/jwks.json` y **rechaza los tokens HS256 del secreto compartido legado**. Si tu proyecto es antiguo, migra a las claves nuevas desde esa misma pantalla.
3. **Project Settings → API Keys**: el simulador solo necesita la **publishable key** (`sb_publishable_...`, o la `anon` legada). La `secret` o `service_role` **nunca** va dentro del juego.

## 2. Cadena de conexión

En **Connect** (arriba en el dashboard) copia la del **Session pooler**. Funciona sobre IPv4, que es lo que tienen Azure Container Apps y la mayoría de redes domésticas:

```
Host=aws-<n>-<region>.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<project-ref>;Password=<password>;SSL Mode=Require
```

- No uses el *Transaction pooler* (puerto 6543) para esta API: es para funciones serverless.
- Guárdala con user-secrets en local o como secret en Azure. **Nunca** en `appsettings.json`.

```bash
dotnet user-secrets set "ConnectionStrings:LabSimulator" "<cadena>" -p src/LabSimulator.Api
```

## 3. Crear el esquema (reconstrucción de la base de datos)

La base original de SQL Server no estaba versionada. Ahora el esquema completo sale de las migraciones de EF Core. Hay dos opciones:

**A. Desde tu máquina** (recomendado):

```bash
dotnet ef database update -p src/LabSimulator.Api --connection "<cadena de conexión>"
```

**B. Desde el SQL Editor de Supabase**: genera el script y pégalo. Es idempotente, así que puedes ejecutarlo varias veces.

```bash
dotnet ef migrations script --idempotent -p src/LabSimulator.Api -o schema.sql
```

La CI también publica ese `schema.sql` como artefacto en cada build.

Resultado: esquema `labsim` con `profiles`, `matches`, `match_participants`, `scenarios`, `scenario_steps` (rúbrica sembrada), `scenario_attempts` y `attempt_step_results`.

> **Por qué un esquema propio:** la Data API de Supabase (PostgREST) solo expone `public`. Al vivir en `labsim`, las tablas no son accesibles con la publishable key; el único acceso es a través de la API.

## 4. Primer administrador

1. Regístrate desde el simulador, o en **Authentication → Users → Add user**.
2. Llama a `PUT /api/v1/me` con ese usuario para crear su perfil.
3. En el SQL Editor:

```sql
update labsim.profiles set role = 'Admin' where id = '<uuid del usuario en Authentication → Users>';
```

A partir de ahí, ese admin asigna el rol `Teacher` con `PUT /api/v1/profiles/{id}/role`.

## Datos personales

Los perfiles guardan solo el nombre visible; el correo vive en Supabase Auth. Si se elimina a un usuario por solicitud de habeas data (Ley 1581), borra también su fila en `labsim.profiles`, y en cascada se eliminan sus intentos y participaciones.
