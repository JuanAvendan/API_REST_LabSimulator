# Publicar en Azure Container Apps

Azure Container Apps (ACA) ejecuta la imagen del `Dockerfile` con HTTPS, escalado automático y un plan de consumo con cuota gratuita mensual.

## Antes de empezar

- Azure CLI con la extensión: `az extension add --name containerapp --upgrade`
- **Región:** pon ACA y el proyecto de Supabase en la misma zona para que cada consulta no cruce continentes. Por ejemplo, Supabase `sa-east-1` (São Paulo) con ACA `brazilsouth`, o Supabase `us-east-1` con ACA `eastus`.

## Primer despliegue

```bash
az group create --name rg-labsimulator --location brazilsouth

az containerapp env create --name cae-labsimulator --resource-group rg-labsimulator --location brazilsouth

# Construye la imagen en Azure Container Registry y crea la app
az containerapp up \
  --name labsimulator-api \
  --resource-group rg-labsimulator \
  --environment cae-labsimulator \
  --source . \
  --ingress external \
  --target-port 8080
```

> `--source .` crea un Azure Container Registry, con un costo mensual pequeño. Si prefieres GitHub Container Registry, publica la imagen allí y usa `--image ghcr.io/<usuario>/labsimulator-api:<tag>`.

## Configuración y secretos

```bash
az containerapp secret set --name labsimulator-api --resource-group rg-labsimulator \
  --secrets db-connection="<cadena del Session pooler de Supabase>"

az containerapp update --name labsimulator-api --resource-group rg-labsimulator \
  --set-env-vars \
    ConnectionStrings__LabSimulator=secretref:db-connection \
    Supabase__ProjectUrl=https://<project-ref>.supabase.co
```

La API valida la configuración al arrancar y no inicia si falta algún valor.

## Health probes

En el portal (**Containers → Health probes**) o en YAML:

| Probe | Ruta | Puerto |
| --- | --- | --- |
| Liveness | `/health/live` | 8080 |
| Readiness | `/health/ready` (verifica la conexión a Postgres) | 8080 |

## Escalado

- `--min-replicas 0`: casi gratis, pero la primera petición tras un rato de inactividad tarda unos segundos (*cold start*).
- `--min-replicas 1`: sin cold start; recomendado durante el horario de clases.

```bash
az containerapp update --name labsimulator-api --resource-group rg-labsimulator --min-replicas 0 --max-replicas 3
```

## Migraciones

Las migraciones **no** se aplican al arrancar la app: así evitas que varias réplicas migren a la vez o que una migración fallida tumbe el servicio. Aplícalas antes de desplegar una versión que las requiera (ver [supabase.md](supabase.md#3-crear-el-esquema-reconstrucción-de-la-base-de-datos)).

## URL para el simulador

```bash
az containerapp show --name labsimulator-api --resource-group rg-labsimulator --query properties.configuration.ingress.fqdn -o tsv
```

Esa URL `https://...azurecontainerapps.io` es la base de la API en el simulador. Tiene un certificado válido, a diferencia de `https://localhost:7069`, que solo funciona en la máquina de desarrollo.
