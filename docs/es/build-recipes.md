# Recetas de build

Las recetas son adaptadores PowerShell por tecnologia. DeployPilot mantiene la orquestacion en .NET y deja los detalles especificos de compilacion en scripts chicos y reemplazables.

## Preparacion Git del agente

Antes de ejecutar una receta, el agente prepara el workspace con Git:

- clona cuando la carpeta del repositorio no existe.
- actualiza tags y ramas en repositorios existentes.
- hace checkout del SHA solicitado si la build apunta a una version historica.
- usa la rama por defecto cuando no se solicita SHA.

`ExecuteGit` viene desactivado por defecto para que las demos locales no dependan de red ni credenciales.

## Deteccion de repositorios

La API expone `POST /api/repositories/probe` para inspeccionar una carpeta local desde el servidor. El detector encuentra proyectos soportados y devuelve sugerencias de receta para precompletar tecnologia y ruta de proyecto.

Archivos detectados:

- `.sln`: MSBuild classic
- `.csproj`: C# WinForms
- `.vbproj`: VB.NET WinForms
- `.pjx` / `.prg`: comando configurable FoxPro
