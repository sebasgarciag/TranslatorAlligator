# Guía de Pruebas con Postman

Este documento contiene ejemplos de solicitudes para probar el Servicio de Traducción utilizando Postman.

## Configuración Básica en Postman

Para todas las pruebas, asegúrate de:

1. La API debe estar corriendo en `http://localhost:5064`
2. Debes configurar correctamente los encabezados según el formato (JSON o XML)

## 1. Prueba Básica de Traducción (JSON)

**Solicitud:**
- Método: `POST`
- URL: `http://localhost:5064/api/Translate`
- Headers:
  - `Content-Type: application/json`
  - `Accept: application/json`
- Body (raw JSON):
```json
{
  "items": [
    {
      "text": "Hello, how are you?",
      "to": "es"
    }
  ]
}
```

**Respuesta Esperada:**
- Status: 200 OK
- Body: JSON con la traducción "¡Hola! ¿Cómo estás?" o similar

## 2. Traducción a Múltiples Idiomas

**Solicitud:**
- Método: `POST`
- URL: `http://localhost:5064/api/Translate`
- Headers:
  - `Content-Type: application/json`
  - `Accept: application/json`
- Body (raw JSON):
```json
{
  "items": [
    {
      "text": "Hello, nice to meet you",
      "to": "es"
    },
    {
      "text": "How is the weather today?",
      "to": "fr"
    },
    {
      "text": "I love programming",
      "to": "de"
    }
  ]
}
```

**Respuesta Esperada:**
- Status: 200 OK
- Body: JSON con traducciones en español, francés y alemán

## 2.1. Prueba con Muchas Traducciones Simultáneas

Esta prueba permite evaluar cómo maneja la API un gran número de traducciones en una sola solicitud:

**Solicitud:**
- Método: `POST`
- URL: `http://localhost:5064/api/Translate`
- Headers:
  - `Content-Type: application/json`
  - `Accept: application/json`
- Body (raw JSON):
```json
{
  "items": [
    { "text": "Good morning", "to": "es" },
    { "text": "Good afternoon", "to": "es" },
    { "text": "Good evening", "to": "es" },
    { "text": "How are you?", "to": "fr" },
    { "text": "My name is John", "to": "fr" },
    { "text": "Nice to meet you", "to": "fr" },
    { "text": "What time is it?", "to": "de" },
    { "text": "Where is the restaurant?", "to": "de" },
    { "text": "I would like to order", "to": "de" },
    { "text": "Thank you very much", "to": "it" },
    { "text": "You're welcome", "to": "it" },
    { "text": "See you tomorrow", "to": "pt" },
    { "text": "Have a nice day", "to": "pt" },
    { "text": "What is your name?", "to": "nl" },
    { "text": "Can you help me?", "to": "nl" }
  ]
}
```

**Respuesta Esperada:**
- Status: 200 OK
- Body: JSON con 15 traducciones en diferentes idiomas (español, francés, alemán, italiano, portugués y holandés)
- Tiempo de respuesta: Mayor que para solicitudes más pequeñas, pero aún razonable (generalmente <10 segundos para la primera solicitud)

**Notas sobre esta prueba:**
- Útil para evaluar el rendimiento y estabilidad del servicio bajo carga
- Permite verificar la capacidad de caché para múltiples entradas
- Si se ejecuta repetidamente, las traducciones ya cacheadas deberían recuperarse rápidamente

## 3. Prueba del Sistema de Caché

Para probar el sistema de caché, envía la misma solicitud dos veces y observa los tiempos de respuesta y los logs:

1. Envía la solicitud básica (como en el ejemplo 1)
2. En los logs, debería indicar "Fallo en caché" y llamar a OpenAI
3. Envía la misma solicitud de nuevo
4. En los logs, debería indicar "Acierto en caché" y la respuesta debería ser casi instantánea

## 4. Prueba con Formato XML

**Solicitud:**
- Método: `POST`
- URL: `http://localhost:5064/api/Translate`
- Headers:
  - `Content-Type: application/xml`
  - `Accept: application/xml`
- Body (raw XML):
```xml
<TranslationRequest>
  <Items>
    <Item>
      <Text>Good morning, welcome to our service</Text>
      <To>es</To>
    </Item>
  </Items>
</TranslationRequest>
```

**Respuesta Esperada:**
- Status: 200 OK
- Body: XML con la traducción

## 5. Prueba de Errores - Solicitud Vacía

**Solicitud:**
- Método: `POST`
- URL: `http://localhost:5064/api/Translate`
- Headers:
  - `Content-Type: application/json`
  - `Accept: application/json`
- Body (raw JSON):
```json
{
  "items": []
}
```

**Respuesta Esperada:**
- Status: 400 Bad Request
- Body: Mensaje de error indicando que la solicitud debe contener al menos un elemento

## 6. Prueba de Errores - Idioma No Soportado

**Solicitud:**
- Método: `POST`
- URL: `http://localhost:5064/api/Translate`
- Headers:
  - `Content-Type: application/json`
  - `Accept: application/json`
- Body (raw JSON):
```json
{
  "items": [
    {
      "text": "Hello world",
      "to": "xyz"
    }
  ]
}
```

**Respuesta Esperada:**
- Status: 200 OK (la API intenta traducir de todos modos)
- La respuesta puede contener un error o una traducción aproximada

## 7. Prueba de Textos Largos

**Solicitud:**
- Método: `POST`
- URL: `http://localhost:5064/api/Translate`
- Headers:
  - `Content-Type: application/json`
  - `Accept: application/json`
- Body (raw JSON):
```json
{
  "items": [
    {
      "text": "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam auctor, nisl eget ultricies lacinia, nisl nisl aliquam nisl, eget aliquam nisl nisl eget nisl. Nullam auctor, nisl eget ultricies lacinia, nisl nisl aliquam nisl, eget aliquam nisl nisl eget nisl. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam auctor, nisl eget ultricies lacinia, nisl nisl aliquam nisl, eget aliquam nisl nisl eget nisl. Nullam auctor, nisl eget ultricies lacinia, nisl nisl aliquam nisl, eget aliquam nisl nisl eget nisl.",
      "to": "es"
    }
  ]
}
```

**Respuesta Esperada:**
- Status: 200 OK
- Body: JSON con la traducción del texto largo

## 8. Prueba de Caracteres Especiales

**Solicitud:**
- Método: `POST`
- URL: `http://localhost:5064/api/Translate`
- Headers:
  - `Content-Type: application/json`
  - `Accept: application/json`
- Body (raw JSON):
```json
{
  "items": [
    {
      "text": "Special characters: !@#$%^&*()_+{}|:<>?~`-=[]\\;',./",
      "to": "es"
    }
  ]
}
```

**Respuesta Esperada:**
- Status: 200 OK
- Body: JSON con los caracteres especiales preservados en la traducción

## 9. Prueba de Aceptación de Diferentes Formatos

**Solicitud JSON, Respuesta XML:**
- Método: `POST`
- URL: `http://localhost:5064/api/Translate`
- Headers:
  - `Content-Type: application/json`
  - `Accept: application/xml`
- Body (raw JSON):
```json
{
  "items": [
    {
      "text": "Hello, this is a test",
      "to": "es"
    }
  ]
}
```

**Respuesta Esperada:**
- Status: 200 OK
- Body: XML con la traducción

## 10. Prueba de Rendimiento (Múltiples Solicitudes)

Para probar el rendimiento, puedes usar la función "Runner" de Postman:

1. Guarda la solicitud básica como una colección
2. Usa el "Runner" para ejecutarla 10-20 veces
3. Observa los tiempos de respuesta para evaluar:
   - Primera solicitud (sin caché): ~1-2 segundos
   - Solicitudes posteriores (con caché): <50ms

## Consejos para Depuración

- Verifica los logs de la aplicación para ver el flujo de procesamiento
- Para ver exactamente cómo se está manejando la caché, busca mensajes como "Fallo en caché" o "Acierto en caché"
- Si hay problemas con OpenAI, verifica los logs para ver la respuesta exacta

## Pruebas de Endpoint de Salud

**Solicitud:**
- Método: `GET`
- URL: `http://localhost:5064/health`

**Respuesta Esperada:**
- Status: 200 OK
- Body: JSON con información sobre el estado del servicio
```json
{
  "status": "Healthy",
  "timestamp": "2023-06-20T15:30:45.123Z"
}
``` 