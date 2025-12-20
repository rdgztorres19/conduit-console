# Aplicación Angular - PLC Monitor

Se ha creado una aplicación Angular dentro del proyecto `ConduitPlcDemo` que permite monitorear y editar tags del PLC en tiempo real.

## 📁 Estructura

```
ConduitPlcDemo/
├── angular-app/              # Aplicación Angular
│   ├── src/
│   │   ├── app/
│   │   │   ├── app.component.ts      # Componente principal
│   │   │   └── tree-node.component.ts # Componente de árbol
│   │   ├── styles.css                # Estilos globales
│   │   ├── index.html
│   │   └── main.ts
│   ├── angular.json
│   ├── package.json
│   └── tsconfig.json
└── wwwroot/                  # Build output (generado automáticamente)
```

## 🚀 Características Implementadas

### ✅ Interfaz de Usuario
- Input para ingresar el nombre del tag (por defecto: `ngpSampleCurrent`)
- Botón "Buscar Estructura" para cargar la estructura del tag
- Visualización en árbol de la estructura de datos
- Indicador de estado de conexión PLC
- Manejo de errores con mensajes claros

### ✅ Funcionalidad en Tiempo Real
- Actualización automática cada segundo
- Detección de cambios en valores
- Animación visual cuando un valor cambia
- Mantiene la estructura del árbol expandida/colapsada

### ✅ Edición de Valores
- Campos editables para valores primitivos (number, string, boolean)
- Botón de escritura para cada valor editable
- Conversión automática de tipos
- Escritura de valores anidados al PLC

### ✅ Diseño
- Interfaz moderna con gradientes y sombras
- Diseño responsivo
- Animaciones suaves
- Indicadores visuales de estado

## 🔧 Configuración

### Build Automático
El proyecto .NET está configurado para construir Angular automáticamente antes de compilar:

```xml
<Target Name="BuildAngular" BeforeTargets="Build">
  <Exec Command="npm install" WorkingDirectory="angular-app" Condition="!Exists('angular-app/node_modules')" />
  <Exec Command="npm run build" WorkingDirectory="angular-app" />
</Target>
```

### Servir Archivos Estáticos
La API está configurada para servir los archivos estáticos desde `wwwroot`:

```csharp
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html"); // Para SPA routing
```

## 📦 Instalación y Build Manual

Si necesitas construir Angular manualmente:

```bash
cd angular-app
npm install
npm run build
```

O usar el script proporcionado:

```bash
./build-angular.sh
```

## 🌐 Acceso

Una vez que la aplicación esté corriendo:

- **Web API**: `http://localhost:5000` o `https://localhost:5001`
- **Angular App**: `http://localhost:5000` (servida desde wwwroot)
- **Swagger UI**: `http://localhost:5000/swagger`

## 📝 Uso

1. Inicia la aplicación .NET: `dotnet run`
2. Abre tu navegador en `http://localhost:5000`
3. Ingresa el nombre del tag (por defecto: `ngpSampleCurrent`)
4. Haz clic en "Buscar Estructura"
5. Observa la estructura en árbol
6. Los valores se actualizan automáticamente cada segundo
7. Edita valores editables y haz clic en el botón ✏️ para escribir al PLC

## 🔌 Endpoints Utilizados

La aplicación Angular consume:

- `GET /api/plc/status` - Estado de conexión PLC
- `GET /api/plc/tags/{tagName}` - Leer tag
- `POST /api/plc/tags/{tagName}` - Escribir tag

## 🎨 Componentes

### AppComponent
- Componente principal que maneja la lógica de negocio
- Gestiona la carga de estructura
- Controla las actualizaciones periódicas
- Maneja la escritura de valores

### TreeNodeComponent
- Componente recursivo para mostrar el árbol
- Maneja la expansión/colapso de nodos
- Permite edición de valores primitivos
- Emite eventos de escritura

## 🐛 Troubleshooting

### Angular no se construye automáticamente
- Verifica que Node.js y npm estén instalados
- Ejecuta manualmente: `cd angular-app && npm install && npm run build`

### La aplicación no carga
- Verifica que `wwwroot` contenga los archivos de build
- Revisa la consola del navegador para errores
- Asegúrate de que la API esté corriendo

### Errores de CORS
- La aplicación Angular se sirve desde la misma API, no debería haber problemas de CORS

### Valores no se actualizan
- Verifica la conexión PLC en el endpoint `/api/plc/status`
- Revisa la consola del navegador para errores de red
- Verifica que el tag existe y tiene calidad "Good"
