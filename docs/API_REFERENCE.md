# Referencia de la API

## API de Traducción

La API de Traducción es un servicio en tiempo real que traduce texto desde inglés a varios idiomas de destino utilizando modelos GPT de OpenAI con caché integrada para optimización de rendimiento.

### URL Base

```
http://localhost:5064/api
```

## Endpoints

### Traducir Texto

Traduce uno o más textos a los idiomas de destino especificados.

**URL**: `/translate`

**Método**: `POST`

**Autenticación requerida**: No

**Permisos requeridos**: Ninguno

**Formatos de solicitud**: JSON o XML

#### Ejemplo de solicitud JSON

```json
{
  "items": [
    {
      "text": "Hello, how are you?",
      "to": "es"
    },
    {
      "text": "The weather is nice today",
      "to": "fr"
    }
  ]
}
```

#### Ejemplo de solicitud XML

```xml
<TranslateRequest>
  <Items>
    <Item>
      <Text>Hello, how are you?</Text>
      <To>es</To>
    </Item>
    <Item>
      <Text>The weather is nice today</Text>
      <To>fr</To>
    </Item>
  </Items>
</TranslateRequest>
```

### Respuesta exitosa

**Código**: `200 OK`

#### Ejemplo de respuesta JSON

```json
{
  "results": [
    {
      "text": "Hello, how are you?",
      "translatedText": "Hola, ¿cómo estás?",
      "to": "es"
    },
    {
      "text": "The weather is nice today",
      "translatedText": "Le temps est beau aujourd'hui",
      "to": "fr"
    }
  ]
}
```

#### Ejemplo de respuesta XML

```xml
<TranslationResponse>
  <Results>
    <Result>
      <Text>Hello, how are you?</Text>
      <TranslatedText>Hola, ¿cómo estás?</TranslatedText>
      <To>es</To>
    </Result>
    <Result>
      <Text>The weather is nice today</Text>
      <TranslatedText>Le temps est beau aujourd'hui</TranslatedText>
      <To>fr</To>
    </Result>
  </Results>
</TranslationResponse>
```

### Respuestas de error

**Condición**: Si la solicitud es malformada o vacía

**Código**: `400 BAD REQUEST`

**Contenido**:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "La solicitud debe contener al menos un elemento para traducir"
}
```

**Condición**: Si ocurre un error en el servidor

**Código**: `500 INTERNAL SERVER ERROR`

**Contenido**:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Error Interno del Servidor",
  "status": 500,
  "detail": "Ocurrió un error al procesar la solicitud de traducción."
}
```

## Idiomas soportados

El servicio admite traducción a cualquier idioma compatible con los modelos de OpenAI. Utilice códigos de idioma estándar (por ejemplo, "es" para español, "fr" para francés, "de" para alemán, etc.).

## Rendimiento

- Aciertos en caché: < 50ms tiempo de respuesta
- Fallos en caché (requiriendo traducción AI): < 2s tiempo de respuesta (típico)

## Notas

- La API detecta automáticamente si está utilizando JSON o XML basándose en el encabezado Content-Type de su solicitud, y responde en el mismo formato.
- Todas las traducciones se almacenan en caché para mejorar el rendimiento de solicitudes repetidas. La expiración predeterminada de caché es de 24 horas.
- El servicio utiliza GPT-3.5 Turbo de OpenAI por defecto, pero puede configurarse para usar otros modelos. 