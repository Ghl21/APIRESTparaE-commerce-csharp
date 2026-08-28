# API REST para E-commerce

API REST de comercio electrónico construida con **C# 12 / .NET 8**, **ASP.NET Core**,
**Entity Framework Core 8**, autenticación **JWT** y **SQL Server** (2012 o superior).

Cubre el flujo completo de una tienda: catálogo, carrito, direcciones, checkout, pagos,
reseñas y administración de usuarios.

---

## 1. Arquitectura

Solución en capas con dependencias en una sola dirección
(`API → Infrastructure → Application → Domain`):

```
ECommerce.sln
├─ src/
│  ├─ ECommerce.Domain          Entidades, enums y excepciones de negocio. Sin dependencias.
│  ├─ ECommerce.Application     DTOs, contratos, servicios de caso de uso y reglas de negocio.
│  ├─ ECommerce.Infrastructure  DbContext, mapeos de EF Core, JWT, hashing y siembra.
│  └─ ECommerce.API             Controladores, middleware, Swagger y configuración.
└─ tests/
   └─ ECommerce.Tests           39 pruebas unitarias (xUnit + EF Core InMemory).
```

Decisiones de diseño relevantes:

- **Sin migraciones de EF.** El esquema lo crean los scripts de
  `../APIRESTparaE-commerce-SqlServer` y el modelo está mapeado explícitamente contra él.
  Así el DBA controla la base y la aplicación no la modifica.
- **Compatibilidad 2012.** El proveedor se configura con `UseCompatibilityLevel(110)` para que
  EF Core no genere T-SQL propio de versiones más nuevas.
- **Sin mapeador automático.** Las conversiones a DTO son explícitas
  (`Application/Mapping/MappingExtensions.cs`), de modo que queda claro qué datos se exponen.
- **Sin paquetes de terceros para seguridad.** El hashing usa PBKDF2 de la BCL, así la
  solución compila sin dependencias externas más allá de EF Core, JwtBearer y Swashbuckle.

---

## 2. Puesta en marcha

### Requisitos

- [.NET SDK 8.0](https://dotnet.microsoft.com/download) o superior
- SQL Server 2012 o superior (Express, Developer o Standard)
- SQL Server Management Studio (para ejecutar los scripts)

### Pasos

**1. Crear la base de datos.** Ejecute en SSMS, en orden, los scripts de
`../APIRESTparaE-commerce-SqlServer` (del `01` al `06`). Vea el README de esa carpeta.

**2. Configurar la cadena de conexión** en `src/ECommerce.API/appsettings.json` o en
`appsettings.Development.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=ECommerceDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Encrypt=False"
}
```

Para SQL Express use `Server=localhost\\SQLEXPRESS`.

**3. Cambiar la clave de firma JWT** (`Jwt:SecretKey`). Debe tener al menos 32 caracteres;
la aplicación se niega a arrancar con una clave más corta. En producción cárguela desde
variables de entorno o desde el gestor de secretos:

```bash
dotnet user-secrets set "Jwt:SecretKey" "una-clave-larga-y-aleatoria-de-mas-de-32-caracteres"
```

**4. Restaurar, compilar y ejecutar:**

```bash
dotnet restore
```

```bash
dotnet run --project src/ECommerce.API
```

**5. Abrir Swagger** en <http://localhost:5080/swagger>.

### Usuarios de prueba

| Correo | Contraseña | Rol |
|--------|-----------|-----|
| `admin@ecommerce.com` | `Admin123$` | Admin |
| `cliente@ecommerce.com` | `Cliente123$` | Customer |

Si la base se creó sin datos, la API siembra al arrancar los roles y el administrador
(controlado por la sección `Seed` de `appsettings.json`).

---

## 3. Autenticación

1. `POST /api/v1/auth/login` devuelve un **access token** (JWT, 60 minutos) y un
   **refresh token** (7 días).
2. Envíe el access token en cada petición protegida:
   `Authorization: Bearer {token}`.
3. Cuando caduque, use `POST /api/v1/auth/refresh`. El refresh token **rota**: el anterior
   queda revocado y apuntando al nuevo, de modo que un token robado y reutilizado se detecta.
4. `POST /api/v1/auth/revoke` cierra la sesión.

En Swagger pulse **Authorize** y pegue únicamente el token (sin la palabra `Bearer`).

El token incluye los claims `sub` (id de usuario), `email`, `name` y `role`.

---

## 4. Endpoints

Prefijo común: `/api/v1`. 🔓 público · 🔒 autenticado · 👑 sólo Admin

### Autenticación (`/auth`)
| Método | Ruta | Acceso | Descripción |
|--------|------|--------|-------------|
| POST | `/auth/register` | 🔓 | Registra un cliente y devuelve sus tokens |
| POST | `/auth/login` | 🔓 | Inicia sesión |
| POST | `/auth/refresh` | 🔓 | Renueva el access token rotando el refresh token |
| POST | `/auth/revoke` | 🔓 | Revoca un refresh token |
| GET | `/auth/me` | 🔒 | Perfil del usuario autenticado |
| POST | `/auth/change-password` | 🔒 | Cambia la contraseña y cierra las demás sesiones |

### Catálogo (`/categories`, `/products`)
| Método | Ruta | Acceso | Descripción |
|--------|------|--------|-------------|
| GET | `/categories` | 🔓 | Lista de categorías con número de productos |
| GET | `/categories/{id}` | 🔓 | Detalle de categoría |
| POST/PUT/DELETE | `/categories...` | 👑 | Alta, edición y baja |
| GET | `/products` | 🔓 | Búsqueda paginada con filtros y orden |
| GET | `/products/{id}` | 🔓 | Detalle con imágenes y valoración |
| GET | `/products/slug/{slug}` | 🔓 | Detalle por slug (URL amigable) |
| POST | `/products` | 👑 | Crear producto |
| PUT | `/products/{id}` | 👑 | Actualizar producto y su galería |
| PATCH | `/products/{id}/stock` | 👑 | Ajustar existencias |
| DELETE | `/products/{id}` | 👑 | Eliminar (o desactivar si ya se vendió) |

Parámetros de `/products`: `search`, `categoryId`, `minPrice`, `maxPrice`, `inStock`,
`isActive`, `pageNumber`, `pageSize`, `sortBy` (`name`, `price`, `stock`, `sku`),
`sortDescending`.

```
GET /api/v1/products?search=laptop&categoryId=4&minPrice=1000&sortBy=price&sortDescending=false&pageNumber=1&pageSize=10
```

### Reseñas
| Método | Ruta | Acceso | Descripción |
|--------|------|--------|-------------|
| GET | `/products/{id}/reviews` | 🔓 | Reseñas aprobadas del producto |
| POST | `/products/{id}/reviews` | 🔒 | Crea o actualiza la reseña propia |
| DELETE | `/reviews/{id}` | 🔒 | Elimina la propia (un Admin, cualquiera) |

### Direcciones (`/addresses`) — 🔒
CRUD completo sobre la libreta del usuario. La primera dirección queda como predeterminada
y, al borrar la predeterminada, se promueve otra automáticamente.

### Carrito (`/cart`) — 🔒
| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/cart` | Carrito con subtotal, IVA y envío estimados |
| POST | `/cart/items` | Agrega un producto o acumula cantidad |
| PUT | `/cart/items/{itemId}` | Cambia la cantidad |
| DELETE | `/cart/items/{itemId}` | Quita una línea |
| DELETE | `/cart` | Vacía el carrito |

### Pedidos (`/orders`) — 🔒
| Método | Ruta | Acceso | Descripción |
|--------|------|--------|-------------|
| GET | `/orders` | 🔒 | Propios; un Admin ve todos y puede filtrar por `userId` |
| GET | `/orders/{id}` | 🔒 | Detalle (sólo el dueño o un Admin) |
| POST | `/orders` | 🔒 | Checkout: convierte el carrito en pedido |
| PATCH | `/orders/{id}/status` | 👑 | Cambia el estado |
| POST | `/orders/{id}/cancel` | 🔒 | Cancela y devuelve el stock |
| GET | `/orders/{id}/payments` | 🔒 | Pagos del pedido |

### Pagos (`/payments`) — 🔒
| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/payments` | Cobra un pedido en estado `Pending` |
| GET | `/payments/order/{orderId}` | Pagos de un pedido |

### Usuarios (`/users`) — 👑
Listado paginado, detalle, activar/desactivar cuentas y asignar o quitar roles.

### Otros
`GET /health` (estado del servicio) y `GET /swagger` (documentación interactiva).

---

## 5. Reglas de negocio implementadas

**Checkout** (`POST /orders`)
1. Revalida que cada producto siga activo y con stock suficiente.
2. Descuenta el stock para reservar la mercancía.
3. Copia el nombre, SKU y precio de cada producto en el pedido, y también la dirección de
   envío: si mañana cambia el catálogo, el histórico de ventas no se altera.
4. Vacía el carrito.
5. Todo ocurre dentro de una transacción ejecutada con la estrategia de reintentos de EF Core.

**Precios** (`Application/Common/PricingRules.cs`): IVA del 16 %, envío fijo de 99,00 y envío
gratuito a partir de 1.500,00 de subtotal. El total estimado del carrito y el definitivo del
pedido usan el mismo código, así que nunca difieren.

**Transiciones de estado permitidas:**

```
Pending    → Paid, Cancelled
Paid       → Processing, Cancelled, Refunded
Processing → Shipped, Cancelled
Shipped    → Delivered
Delivered  → Refunded
Cancelled  → (final)
Refunded   → (final)
```

Cancelar o reembolsar devuelve las unidades al inventario. Un pedido enviado o entregado no
se puede cancelar.

**Pagos:** la pasarela es simulada y no contacta con ningún proveedor real. Sólo se guardan
los últimos cuatro dígitos de la tarjeta. Para pruebas, **las tarjetas terminadas en `0000`
se rechazan** y el resto se aprueban; `CashOnDelivery` deja el pago pendiente y el pedido sin
cambiar de estado.

**Concurrencia:** `catalog.Products` tiene una columna `RowVersion`. Si dos compras
simultáneas tocan el mismo producto, la segunda recibe `409 Conflict` en vez de dejar el
stock en negativo.

---

## 6. Seguridad

- **Contraseñas:** PBKDF2-HMAC-SHA256, 100.000 iteraciones, sal aleatoria de 16 bytes y
  comparación en tiempo constante (`Infrastructure/Security/PasswordHasher.cs`).
- **Login:** el mismo mensaje para usuario inexistente y contraseña incorrecta, para no
  revelar qué correos están registrados.
- **Autorización por roles** mediante políticas (`AdminOnly`, `CustomerOnly`) y verificación
  de propiedad en cada recurso: un cliente no puede leer ni cancelar pedidos ajenos.
- **Rotación de refresh tokens** y revocación masiva al cambiar la contraseña o desactivar
  una cuenta.
- **Rate limiting** por IP (120 peticiones por minuto, configurable en `RateLimiting`).
- **CORS** restringible por origen desde `Cors:AllowedOrigins`.
- **Errores uniformes:** un middleware traduce las excepciones de dominio a códigos HTTP y
  oculta los detalles internos fuera de Development.

---

## 7. Formato de las respuestas

Listados paginados:

```json
{
  "items": [ ... ],
  "pageNumber": 1,
  "pageSize": 10,
  "totalRecords": 42,
  "totalPages": 5,
  "hasPrevious": false,
  "hasNext": true
}
```

Errores:

```json
{
  "statusCode": 400,
  "title": "Error de validación",
  "message": "Uno o más campos enviados no son válidos.",
  "traceId": "0HNNVV0PEU64J:00000001",
  "timestamp": "2026-08-22T02:51:19.2087960Z",
  "errors": { "CardNumber": ["El número de tarjeta no es válido."] }
}
```

| Código | Cuándo se devuelve |
|--------|--------------------|
| 400 | Validación de datos o regla de negocio incumplida |
| 401 | Falta el token, está caducado o las credenciales son incorrectas |
| 403 | Autenticado pero sin permisos sobre el recurso |
| 404 | El recurso no existe |
| 409 | Duplicado (SKU, correo) o conflicto de concurrencia |
| 429 | Se superó el límite de peticiones |

---

## 8. Configuración

| Clave | Descripción | Valor por defecto |
|-------|-------------|-------------------|
| `ConnectionStrings:DefaultConnection` | Cadena de conexión a SQL Server | `Server=localhost;Database=ECommerceDb;...` |
| `Jwt:SecretKey` | Clave de firma HS256 (mínimo 32 caracteres) | *debe cambiarse* |
| `Jwt:AccessTokenExpirationMinutes` | Vigencia del access token | `60` |
| `Jwt:RefreshTokenExpirationDays` | Vigencia del refresh token | `7` |
| `Seed:Enabled` | Sembrar roles y admin al arrancar | `true` |
| `Seed:AdminEmail` / `Seed:AdminPassword` | Credenciales del admin sembrado | `admin@ecommerce.com` / `Admin123$` |
| `Cors:AllowedOrigins` | Orígenes permitidos (`["*"]` para todos) | `["*"]` |
| `RateLimiting:PermitLimit` / `WindowMinutes` | Límite de peticiones por IP | `120` / `1` |
| `Swagger:Enabled` | Publicar Swagger fuera de Development | `true` |
| `Database:EnableSensitiveDataLogging` | Registrar parámetros SQL (sólo para depurar) | `false` |

---

## 9. Pruebas

```bash
dotnet test
```

39 pruebas unitarias sobre base en memoria: hashing de contraseñas, generación de slugs,
reglas de precios, carrito, checkout, transiciones de estado, cancelación con reposición de
stock y la pasarela de pagos. `SeedDataHashTests` además verifica que los hashes escritos en
`04_DatosIniciales.sql` siguen correspondiendo a las contraseñas documentadas.

---

## 10. Comprobado de punta a punta

El proyecto se validó contra una instancia real de SQL Server: se ejecutaron los scripts,
se levantó la API y se recorrió el flujo completo — login con el usuario sembrado por SQL,
búsqueda en el catálogo, alta en el carrito, checkout, pago aprobado, descuento de stock,
cambio de estado por un administrador y rechazo de una transición inválida — verificando
también que los procedimientos almacenados leen correctamente lo que escribe la API.
