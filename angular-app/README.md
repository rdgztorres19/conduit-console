# Angular PLC Monitor App

Aplicación Angular para monitorear y editar tags del PLC en tiempo real.

## Características

- 🔍 Buscar estructura de tags del PLC
- 🌳 Visualización en árbol de la estructura de datos
- ⏱️ Actualización automática cada segundo
- ✏️ Edición de valores directamente desde la interfaz
- 🎨 Interfaz moderna y responsiva

## Desarrollo

### Prerequisitos

- Node.js 18+ y npm
- Angular CLI 17+

### Instalación

```bash
cd angular-app
npm install
```

### Ejecutar en modo desarrollo

```bash
npm start
```

La aplicación estará disponible en `http://localhost:4200`

### Build para producción

```bash
npm run build
```

El build se generará en `../wwwroot` para ser servido por la API de ASP.NET Core.

## Uso

1. Ingresa el nombre del tag (por defecto: `ngpSampleCurrent`)
2. Haz clic en "Buscar Estructura"
3. La estructura se mostrará como un árbol
4. Los valores se actualizan automáticamente cada segundo
5. Los valores editables pueden ser modificados y escritos de vuelta al PLC

## Integración con la API

La aplicación consume los siguientes endpoints:

- `GET /api/plc/status` - Estado de la conexión PLC
- `GET /api/plc/tags/{tagName}` - Leer un tag
- `POST /api/plc/tags/{tagName}` - Escribir un tag
