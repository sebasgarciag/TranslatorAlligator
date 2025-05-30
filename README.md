# Servicio de Traducción

API de traducción en tiempo real construida con .NET que aprovecha modelos de inteligencia artificial para traducir textos con un sistema de caché integrado para mejorar el rendimiento.

## Características

- Traducción de texto en tiempo real desde inglés a cualquier idioma de destino
- Soporte para formatos de solicitud/respuesta tanto en JSON como en XML
- Sistema de caché eficiente para mejorar el rendimiento
- Integración con OpenAI configurable con reintentos automáticos y circuit breaking
- Arquitectura limpia con inyección de dependencias
- Manejo integral de errores y registro de eventos
- Soporte para Docker y contenedores
- Pipeline CI/CD para pruebas y despliegue automatizados

## Arquitectura

La solución sigue los principios de Clean Architecture (Arquitectura Limpia), organizando el código en capas concéntricas donde las dependencias apuntan hacia el interior. Esto permite un alto grado de desacoplamiento y facilita las pruebas y la mantenibilidad del código.

### Capas de la Arquitectura

- **Translator.Core** (Capa de Dominio):
  - Contiene las entidades de negocio, interfaces y lógica de dominio
  - No tiene dependencias externas, es la capa más estable
  - Define los modelos `TranslationRequest`, `TranslationResponse` y sus clases asociadas
  - Establece contratos a través de interfaces como `ICacheProvider` e `IAITranslationClient`
  - Independiente de frameworks y bibliotecas externas

- **Translator.Application** (Capa de Aplicación):
  - Implementa la lógica de negocio y coordina el flujo de datos
  - Contiene los servicios que implementan casos de uso específicos
  - `TranslationService` orquesta el proceso de traducción
  - Depende solo de la capa Core, no de infraestructura
  - Aplica reglas de negocio y maneja excepciones de dominio

- **Translator.Infrastructure** (Capa de Infraestructura):
  - Implementa las interfaces definidas en el Core
  - Proporciona implementaciones concretas para servicios externos
  - `MemoryCacheProvider` implementa `ICacheProvider` usando IMemoryCache de ASP.NET
  - `OpenAITranslationClient` implementa `IAITranslationClient` usando HttpClient
  - Encapsula detalles técnicos como HTTP, caché y configuración

- **Translator.Api** (Capa de Presentación):
  - Expone la funcionalidad como una API REST
  - Gestiona la serialización/deserialización de objetos entre formatos XML/JSON
  - Contiene controladores, middlewares y configuración de aplicación
  - Implementa el manejo de errores HTTP y validación de solicitudes
  - Inyecta dependencias y configura el pipeline de ASP.NET Core

### Patrones Implementados

- **Inversión de Dependencias (DI)**: Las capas externas dependen de abstracciones, no de implementaciones concretas
- **Repositorio y Unidad de Trabajo**: Para el acceso a datos y operaciones de caché
- **Mediador**: Para la comunicación entre componentes respetando el aislamiento
- **Decorador**: Para añadir funcionalidades como logging y caché a servicios existentes
- **Strategy**: Para permitir diferentes estrategias de traducción y caché

### Serialización y Deserialización

La serialización y deserialización son procesos fundamentales en esta API para convertir entre diferentes formatos de datos:

**¿Qué es la Serialización?**
- Es el proceso de convertir objetos de .NET (clases, estructuras) en formatos que pueden ser transmitidos o almacenados
- En esta API, serializamos objetos `TranslationResponse` a JSON o XML para enviarlos al cliente
- Permite que las estructuras de datos complejas sean representadas como texto plano

**¿Qué es la Deserialización?**
- Es el proceso inverso: convertir datos en formatos como JSON o XML a objetos .NET
- La API deserializa las solicitudes entrantes (`TranslationRequest`) del formato de entrada al modelo .NET
- Permite reconstruir objetos con sus propiedades y relaciones a partir de texto

**Implementación en el proyecto:**

1. **Para JSON**:
   - Utilizamos `System.Text.Json` para serializar/deserializar
   - Configurado con opciones para manejar propiedades en camelCase
   - Ejemplo: `JsonSerializer.Deserialize<TranslationRequest>(requestBody, jsonOptions)`

2. **Para XML**:
   - Empleamos `XmlSerializer` de .NET
   - Los modelos están decorados con atributos como `[XmlRoot]`, `[XmlElement]` y `[XmlArray]`
   - Ejemplo: `new XmlSerializer(typeof(TranslationRequest)).Deserialize(reader)`

3. **Detección automática**:
   - El formato se detecta basándose en el encabezado `Content-Type`
   - Se responde en el mismo formato que se recibió la solicitud
   - Permite interoperabilidad con diferentes sistemas

La API determina el formato apropiado basándose en encabezados HTTP como `Content-Type` y `Accept`, lo que proporciona flexibilidad para integrarse con diversos sistemas, tanto modernos (JSON) como legados (XML).

### Flujo de Datos en la Arquitectura

1. La solicitud ingresa a través de `TranslateController` en la capa de API
2. Se deserializa en un objeto `TranslationRequest`
3. El controlador llama al `TranslationService` en la capa de Aplicación
4. El servicio consulta la caché a través de `ICacheProvider`
5. Si no hay acierto en caché, se comunica con `IAITranslationClient`
6. Los resultados se almacenan en caché y se devuelven como `TranslationResponse`
7. La respuesta se serializa al formato adecuado y se envía al cliente

Esta separación clara permite sustituir componentes individuales sin afectar al resto del sistema, facilitando la evolución y el mantenimiento del proyecto.

## Funcionamiento

### Procesamiento de Solicitudes

1. **Recepción de Solicitudes**: La API recibe solicitudes HTTP POST en el endpoint `/api/translate`
   - Formato JSON o XML según el encabezado `Content-Type`
   - Detecta automáticamente el formato y responde en el mismo

2. **Deserialización**: 
   - Las solicitudes JSON se procesan mediante System.Text.Json
   - Las solicitudes XML se procesan mediante XmlSerializer
   - Los atributos XmlRoot, XmlArray y XmlElement garantizan la correcta deserialización XML

3. **Procesamiento**:
   - Para cada elemento de texto a traducir, se crea una clave de caché normalizada
   - Se consulta primero en la caché para optimizar el rendimiento
   - Si no hay coincidencia, se envía la solicitud a la API de OpenAI
   - Los resultados se almacenan en caché para futuras consultas

### Sistema de Caché

El sistema de caché es un componente clave para mejorar el rendimiento y reducir costos:

1. **Cómo funciona**:
   - Utiliza IMemoryCache de ASP.NET Core para almacenamiento en memoria
   - Normaliza las claves de caché para mejorar la tasa de aciertos (trim y ToLowerInvariant)
   - El tiempo de vida (TTL) de caché es configurable (predeterminado: 24 horas)

2. **Rendimiento**:
   - Solicitudes con acierto en caché: <1ms (típicamente 0.05-0.1ms)
   - Solicitudes sin caché: ~1-2 segundos (depende de la respuesta de OpenAI)
   - Mejora de rendimiento: 10,000-20,000 veces más rápido con caché

3. **Formato de clave de caché**:
   - Estructura: `"{texto_normalizado}|{idioma_destino_normalizado}"`
   - Ejemplo: `"Hello, how are you?|es"`

### Integración con OpenAI

La API utiliza el servicio ChatCompletion de OpenAI para realizar traducciones de alta calidad:

1. **Configuración**:
   - Clave API configurable mediante appsettings.json o variables de entorno
   - Modelo configurable (predeterminado: "gpt-3.5-turbo")
   - Sistema de reintentos con retroceso exponencial para manejar errores transitorios

2. **Solicitud a OpenAI**:
   - Utiliza instrucción de sistema para optimizar la calidad de la traducción
   - Formato de prompt: `"Translate the following sentence into {idioma_destino}: '{texto_origen}'"`
   - Temperatura baja (0.3) para maximizar precisión y consistencia

3. **Procesamiento de Respuesta**:
   - Extracción manual de JSON para mayor robustez
   - Manejo de errores detallado con registro de eventos

## Empezando

### Prerrequisitos

- SDK de .NET 7.0 o posterior
- Docker (opcional, para despliegue en contenedores)
- Clave API de OpenAI

### Configuración del Entorno

#### Usando CLI de .NET

1. Clonar el repositorio:
   ```
   git clone https://github.com/sebasgarciag/TranslatorAlligator.git
   ```

2. Navegar al directorio del proyecto:
   ```
   cd TranslatorAlligator
   ```

3. Ejecutar la aplicación:
   ```
   dotnet run --project src/Translator.Api
   ```

#### Usando Docker

1. Clonar el repositorio:
   ```
   git clone https://github.com/sebasgarciag/TranslatorAlligator.git
   ```

2. Construir y ejecutar con Docker Compose (recomendado):
   ```
   docker-compose up -d
   ```
   
   Esto construirá la imagen y ejecutará el contenedor en modo desacoplado (detached).

   Para ver los logs mientras se ejecuta:
   ```
   docker-compose logs -f
   ```

   Para detener el servicio cuando hayas terminado:
   ```
   docker-compose down
   ```

3. Alternativamente, construir la imagen manualmente:
   ```
   docker build -t servicio-traductor -f docker/Dockerfile .
   ```

4. Y ejecutar el contenedor:
   ```
   docker run -p 8080:80 servicio-traductor
   ```

#### Probando el servicio con Postman

Una vez que el servicio esté en ejecución, puedes probarlo utilizando Postman u otra herramienta similar:

1. **Para probar el endpoint de salud**:
   - Método: GET
   - URL: `http://localhost:8080/health`
   - Respuesta esperada:
     ```json
     {
       "status": "Healthy",
       "timestamp": "2025-04-24T22:52:27.0471462Z"
     }
     ```
   - El campo `timestamp` indica la fecha y hora UTC en formato ISO 8601 cuando se procesó la solicitud.

2. **Para realizar una traducción**:
   - Método: POST
   - URL: `http://localhost:8080/api/translate`
   - Headers:
     - Content-Type: `application/json`
     - Accept: `application/json`
   - Body (ejemplo):
     ```json
     {
       "items": [
         {
           "text": "Hello world",
           "to": "es"
         },
         {
           "text": "Good morning",
           "to": "fr"
         }
       ]
     }
     ```
   - Respuesta esperada:
     ```json
     {
       "results": [
         {
           "text": "Hello world",
           "translatedText": "Hola mundo",
           "to": "es"
         },
         {
           "text": "Good morning",
           "translatedText": "Bonjour",
           "to": "fr"
         }
       ]
     }
     ```

La API utilizará automáticamente la clave API de OpenAI incluida en la configuración de desarrollo (appsettings.Development.json), por lo que no es necesario proporcionarla al ejecutar el contenedor.

### Configuración

La aplicación está configurada para utilizar el archivo `appsettings.Development.json` como fuente principal de configuración para el entorno de desarrollo. La clave API de OpenAI ya está incluida en este archivo, por lo que no es necesario configurarla manualmente:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "OpenAI": {
    "ApiKey": "tu-clave-api-aquí",
    "ModelName": "gpt-3.5-turbo"
  }
}
```

Principales variables configurables:

- **OpenAI:ApiKey**: Tu clave API de OpenAI (ya configurada en appsettings.Development.json)
- **OpenAI:ModelName**: Modelo a utilizar para traducciones (predeterminado: "gpt-3.5-turbo")
- **Translation:CacheTTL**: Duración de caché para traducciones (predeterminado: 24 horas)

Al utilizar Docker, el contenedor ya incluye estas configuraciones y no se requiere ninguna acción adicional.

## Uso

La API acepta solicitudes HTTP POST a `/api/translate` con cargas útiles JSON o XML.

### Ejemplo en JSON

```json
POST /api/translate
Content-Type: application/json
Accept: application/json

{
  "items": [
    {
      "text": "Hello",
      "to": "es"
    },
    {
      "text": "How are you?",
      "to": "fr"
    }
  ]
}
```

### Ejemplo en XML

```xml
POST /api/translate
Content-Type: application/xml
Accept: application/xml

<TranslateRequest>
  <Items>
    <Item>
      <Text>Hello</Text>
      <To>es</To>
    </Item>
    <Item>
      <Text>How are you?</Text>
      <To>fr</To>
    </Item>
  </Items>
</TranslateRequest>
```

Para más ejemplos y documentación detallada de la API, consulta [Referencia de API](docs/API_REFERENCE.md).

### Endpoint de Monitoreo de Salud

La API también proporciona un endpoint de monitoreo de salud que es útil para sistemas de orquestación, balanceadores de carga y monitoreo de disponibilidad:

```
GET /health
```

**Respuesta Esperada:**
- Código de Estado: 200 OK
- Cuerpo: JSON con información sobre el estado del servicio
  ```json
  {
    "status": "Healthy",
    "timestamp": "2023-06-20T15:30:45.123Z"
  }
  ```

Este endpoint es utilizado por:
- Docker/Kubernetes para comprobar que el contenedor está funcionando correctamente
- Sistemas de monitoreo para verificar la disponibilidad del servicio
- Balanceadores de carga para determinar si deben enviar tráfico a la instancia

## Pruebas

El proyecto contiene una **suite completa de pruebas automatizadas** que cubre todas las capas de la aplicación con **50 pruebas** que garantizan la calidad y confiabilidad del código.

### 📊 Estadísticas de Pruebas

- **Total de Pruebas**: 50 ✅
- **Pruebas Exitosas**: 50 ✅
- **Pruebas Fallidas**: 0 ✅
- **Cobertura**: Todas las capas (Core, Application, Infrastructure, API)

### 🏗️ Estructura de Tests

```
tests/
├── Translator.Core.Tests/           # 10 pruebas - Modelos y serialización
├── Translator.Application.Tests/    # 13 pruebas - Lógica de negocio
├── Translator.Infrastructure.Tests/ # 17 pruebas - Cache y clientes externos
└── Translator.Api.Tests/           # 10 pruebas - Controladores y endpoints
```

#### **Translator.Core.Tests** - Modelos de Dominio
- ✅ Serialización/deserialización XML de `TranslationRequest` y `TranslationResponse`
- ✅ Serialización/deserialización JSON con formato camelCase
- ✅ Manejo de valores nulos y caracteres especiales
- ✅ Validación de estructura de datos

#### **Translator.Application.Tests** - Lógica de Negocio
- ✅ Servicio de traducción con cache hits/misses
- ✅ Manejo exhaustivo de errores y excepciones
- ✅ Validación de entrada y normalización de texto
- ✅ Integración con cliente AI y sistema de cache
- ✅ Verificación de logging y métricas

#### **Translator.Infrastructure.Tests** - Infraestructura (NUEVO)
- ✅ **Cache Provider**: Operaciones de cache, TTL, manejo de errores
- ✅ **OpenAI Client**: Comunicación HTTP, respuestas exitosas/fallidas
- ✅ Configuración de headers y autenticación
- ✅ Manejo de JSON malformado y timeouts

#### **Translator.Api.Tests** - Controladores
- ✅ **TranslateController**: Respuestas correctas y manejo de errores
- ✅ **HealthController**: Endpoint de salud con timestamps UTC
- ✅ Validación de códigos de estado HTTP
- ✅ Serialización de respuestas

### 🔧 Ejecución de Tests

```bash
# Ejecutar todas las pruebas
dotnet test

# Ejecutar con verbosidad detallada
dotnet test --verbosity normal

# Ejecutar pruebas de un proyecto específico
dotnet test tests/Translator.Infrastructure.Tests/

# Ejecutar una prueba específica
dotnet test --filter "FullyQualifiedName=Translator.Core.Tests.JsonSerializationTests.TranslationRequest_JsonSerialization_SerializesAndDeserializesCorrectly"
```

### 🎯 Tipos de Pruebas Implementadas

#### **Pruebas Unitarias**
- Componentes aislados con mocks para dependencias
- Validación de lógica de negocio
- Manejo de casos edge y errores

#### **Pruebas de Integración**
- Comunicación entre capas
- Serialización/deserialización completa
- Flujos de datos end-to-end

#### **Pruebas de Manejo de Errores**
- Excepciones de servicios externos (OpenAI, Cache)
- Entrada inválida o malformada
- Timeouts y errores de red
- Logging de errores

### 📋 Escenarios de Prueba Cubiertos

- ✅ **Casos de Éxito**: Traducciones exitosas, cache funcionando
- ✅ **Casos de Error**: Servicios no disponibles, datos inválidos
- ✅ **Casos Edge**: Texto vacío, caracteres especiales, listas grandes
- ✅ **Validación**: Constructores, configuración, parámetros nulos

### 📚 Documentación Detallada

Para información completa sobre la suite de pruebas, incluyendo ejemplos de código y mejores prácticas, consulta:

**[📖 Guía Completa de Pruebas](docs/TESTING_GUIDE.md)**

Los tests están diseñados para verificar el correcto funcionamiento de todos los componentes en aislamiento y en integración, utilizando mocks para las dependencias externas como el cliente de OpenAI y el sistema de caché.

## Despliegue

El proyecto incluye un pipeline CI/CD mediante GitHub Actions para compilación, prueba y despliegue automatizados. Consulta `.github/workflows/ci.yml` para detalles. 