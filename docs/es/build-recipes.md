# Recetas de build

Las recetas son adaptadores PowerShell por tecnologia. DeployPilot mantiene la orquestacion en .NET y deja los detalles especificos de compilacion en scripts chicos y reemplazables.

## Deteccion de repositorios

La API expone `POST /api/repositories/probe` para inspeccionar una carpeta local desde el servidor. El detector encuentra proyectos soportados y devuelve sugerencias de receta para precompletar tecnologia y ruta de proyecto.

Archivos detectados:

- `.sln`: MSBuild classic
- `.csproj`: C# WinForms
- `.vbproj`: VB.NET WinForms
- `.pjx` / `.prg`: comando configurable FoxPro
