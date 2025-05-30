# Guía de Pruebas - API Translator

Esta guía documenta la suite completa de pruebas automatizadas del proyecto API Translator.

## 📋 Resumen de la Suite de Pruebas

### Estadísticas Actuales
- **Total de Pruebas**: 50
- **Pruebas Exitosas**: 50 ✅
- **Pruebas Fallidas**: 0 ✅
- **Cobertura**: Todas las capas de la aplicación

### Estructura de Pruebas por Proyecto

```
tests/
├── Translator.Core.Tests/           # 10 pruebas
├── Translator.Application.Tests/    # 13 pruebas  
├── Translator.Infrastructure.Tests/ # 17 pruebas
└── Translator.Api.Tests/           # 10 pruebas
```

## 🏗️ Pruebas por Capa

### 1. Core Tests (Translator.Core.Tests)

#### Pruebas de Serialización XML
- **`UnitTest1.cs`**: Pruebas de serialización XML (5 pruebas)
  - Serialización/deserialización de `TranslationRequest`
  - Serialización/deserialización de `TranslationResponse`
  - Manejo de listas vacías
  - Validación de estructura XML

#### Pruebas de Serialización JSON
- **`JsonSerializationTests.cs`**: Pruebas de serialización JSON (8 pruebas)
  - ✅ Serialización/deserialización completa de objetos
  - ✅ Formato camelCase en JSON
  - ✅ Manejo de valores nulos
  - ✅ Caracteres especiales en texto
  - ✅ Ejemplos de API real
  - ✅ Listas vacías

```csharp
// Ejemplo de prueba JSON
[Fact]
public void TranslationRequest_JsonSerialization_SerializesAndDeserializesCorrectly()
{
    var request = new TranslationRequest
    {
        Items = new List<TranslationItem>
        {
            new TranslationItem { Text = "Hello", To = "es" }
        }
    };
    
    var json = JsonSerializer.Serialize(request, _jsonOptions);
    var deserializedRequest = JsonSerializer.Deserialize<TranslationRequest>(json, _jsonOptions);
    
    Assert.NotNull(deserializedRequest);
    Assert.Equal("Hello", deserializedRequest.Items[0].Text);
}
```

### 2. Application Tests (Translator.Application.Tests)

#### Pruebas del Servicio de Traducción
- **`TranslationServiceTests.cs`**: Pruebas básicas del servicio (3 pruebas)
  - Cache hit/miss scenarios
  - Integración con AI client

#### Pruebas de Manejo de Errores
- **`TranslationServiceErrorHandlingTests.cs`**: Pruebas exhaustivas de errores (10 pruebas)
  - ✅ Excepciones del cliente AI
  - ✅ Excepciones del cache
  - ✅ Entrada nula o vacía
  - ✅ Normalización de texto con espacios
  - ✅ Errores mixtos en múltiples elementos
  - ✅ Validación de constructores
  - ✅ Verificación de logging

```csharp
// Ejemplo de prueba de manejo de errores
[Fact]
public async Task TranslateAsync_WithAIClientException_ReturnsErrorResult()
{
    _mockAiClient.Setup(x => x.TranslateTextAsync("Hello", "es"))
        .ThrowsAsync(new InvalidOperationException("AI service error"));
    
    var result = await service.TranslateAsync(items);
    
    Assert.StartsWith("ERROR:", result[0].TranslatedText);
    Assert.Contains("AI service error", result[0].TranslatedText);
}
```

### 3. Infrastructure Tests (Translator.Infrastructure.Tests)

Este proyecto proporciona cobertura de pruebas para la capa de infraestructura.

#### Cache Provider Tests
- **`MemoryCacheProviderTests.cs`**: Pruebas del proveedor de cache (8 pruebas)
  - ✅ Recuperación exitosa de cache
  - ✅ Cache miss scenarios
  - ✅ Manejo de excepciones con logging
  - ✅ Configuración de TTL
  - ✅ Validación de constructores

```csharp
// Ejemplo de prueba de cache
[Fact]
public async Task GetAsync_WithExistingKey_ReturnsValue()
{
    var key = "test-key";
    var expectedValue = "test-value";
    object? cacheValue = expectedValue;

    _mockCache.Setup(x => x.TryGetValue(key, out cacheValue))
        .Returns(true);

    var result = await _cacheProvider.GetAsync<string>(key);

    Assert.Equal(expectedValue, result);
}
```

#### OpenAI Client Tests
- **`OpenAITranslationClientTests.cs`**: Pruebas del cliente OpenAI (9 pruebas)
  - ✅ Respuestas exitosas de traducción
  - ✅ Remoción de comillas en respuestas
  - ✅ Manejo de errores HTTP
  - ✅ Respuestas vacías
  - ✅ JSON malformado
  - ✅ Configuración de headers y base address
  - ✅ Validación de constructores

```csharp
// Ejemplo de prueba de cliente OpenAI
[Fact]
public async Task TranslateTextAsync_WithSuccessfulResponse_ReturnsTranslation()
{
    var openAIResponse = new
    {
        choices = new[]
        {
            new { message = new { content = "Hola" } }
        }
    };

    _mockHttpHandler.SetupAnyRequest()
        .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(openAIResponse))
        });

    var result = await _client.TranslateTextAsync("Hello", "es");

    Assert.Equal("Hola", result);
}
```

### 4. API Tests (Translator.Api.Tests)

#### Pruebas del Controlador de Traducción
- **`TranslateControllerTests.cs`**: Pruebas del controlador principal (4 pruebas)
  - Respuestas correctas para solicitudes válidas
  - Manejo de errores y respuestas de error
  - Validación de solicitudes

#### Pruebas del Controlador de Salud
- **`HealthControllerTests.cs`**: Pruebas del endpoint de salud (6 pruebas)
  - ✅ Respuesta OK del endpoint
  - ✅ Estado "Healthy" correcto
  - ✅ Timestamp actual en UTC
  - ✅ Timestamps diferentes en múltiples llamadas
  - ✅ Código de estado HTTP 200

```csharp
// Ejemplo de prueba de health endpoint
[Fact]
public void Get_ReturnsHealthyStatus()
{
    var result = _controller.Get() as OkObjectResult;
    var healthResponse = result?.Value;
    
    var statusProperty = healthResponse.GetType().GetProperty("Status");
    var status = statusProperty.GetValue(healthResponse)?.ToString();
    
    Assert.Equal("Healthy", status);
}
```

## 🔧 Configuración de Pruebas

### Dependencias de Testing
Todas las pruebas utilizan las siguientes dependencias:

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
<PackageReference Include="xunit" Version="2.9.2" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
<PackageReference Include="Moq" Version="4.20.70" />
<PackageReference Include="Moq.Contrib.HttpClient" Version="1.4.0" />
<PackageReference Include="coverlet.collector" Version="6.0.2" />
```

### Ejecutar Todas las Pruebas

```bash
# Ejecutar todas las pruebas
dotnet test

# Ejecutar con verbosidad detallada
dotnet test --verbosity normal

# Ejecutar pruebas de un proyecto específico
dotnet test tests/Translator.Infrastructure.Tests/
```

## 📊 Cobertura de Pruebas

### Escenarios Cubiertos

#### ✅ Casos de Éxito
- Traducciones exitosas
- Cache hits y misses
- Serialización JSON/XML
- Endpoints de API funcionando

#### ✅ Manejo de Errores
- Excepciones de servicios externos
- Entrada inválida o nula
- Errores de red y timeouts
- JSON/XML malformado

#### ✅ Casos Edge
- Texto con espacios en blanco
- Caracteres especiales
- Listas vacías
- Múltiples traducciones simultáneas

#### ✅ Validación
- Constructores con parámetros nulos
- Configuración de headers HTTP
- Logging de errores
- TTL de cache

## 🎯 Tipos de Pruebas Implementadas

### **Pruebas Unitarias**
- Componentes aislados con mocks para dependencias
- Validación de lógica de negocio
- Manejo de casos edge y errores

### **Pruebas de Integración**
- Comunicación entre capas
- Serialización/deserialización completa
- Flujos de datos end-to-end

### **Pruebas de Manejo de Errores**
- Excepciones de servicios externos (OpenAI, Cache)
- Entrada inválida o malformada
- Timeouts y errores de red
- Logging de errores

## 📈 Métricas de Calidad

### Estado Actual de las Pruebas
- **Total de Pruebas**: 50
- **Pruebas Exitosas**: 50 ✅
- **Pruebas Fallidas**: 0 ✅
- **Cobertura**: Completa en todas las capas

### Distribución por Proyecto
| Proyecto | Archivos | Pruebas | Cobertura |
|----------|----------|---------|-----------|
| Core.Tests | 2 | 10 | Modelos y serialización |
| Application.Tests | 2 | 13 | Lógica de negocio |
| Infrastructure.Tests | 2 | 17 | Cache y clientes externos |
| Api.Tests | 2 | 10 | Controladores |

## 🎯 Mantenimiento

### Ejecución Regular
- Ejecutar pruebas en cada commit
- Revisar y actualizar mocks cuando cambien las dependencias
- Mantener las pruebas actualizadas con nuevas funcionalidades

### Mejores Prácticas Implementadas
- Uso consistente de patrones AAA (Arrange, Act, Assert)
- Mocking apropiado de dependencias externas
- Verificación de logging y manejo de errores
- Pruebas independientes y determinísticas

---

**Nota**: Esta suite de pruebas proporciona una base sólida para el desarrollo continuo y garantiza la calidad del código en todas las capas de la aplicación. 