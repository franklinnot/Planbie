# 🌱 Planbie - Monitor Ambiental para Plantas

Una aplicación de escritorio moderna para monitorear las condiciones ambientales de tus plantas en tiempo real. Desarrollada con WPF y .NET 8.0, Planbie ofrece una interfaz intuitiva para el seguimiento de temperatura y salud de plantas.

## ✨ Características

- **📊 Monitoreo de Temperatura en Tiempo Real**: Visualización circular con gradientes coloridos que muestra la temperatura actual
- **🌿 Estado de Salud de Plantas**: Indicadores visuales SVG que muestran el estado actual de tu planta
- **📈 Gráficos Históricos**: Visualización de temperatura a lo largo del tiempo con etiquetas temporales personalizadas 
- **🎛️ Controles LED**: Sistema de indicadores LED de colores (rojo, amarillo, azul)
- **🔊 Control de Buzzer**: Gestión de alertas sonoras integrada
- **🖥️ Interfaz Sin Bordes**: Diseño moderno y limpio con controles de ventana personalizados

## 🛠️ Tecnologías Utilizadas

- **.NET 8.0** con WPF para la interfaz de usuario
- **HandyControl 3.5.1** - Controles UI mejorados y layouts flexibles
- **LiveCharts.Wpf 0.9.7** - Visualización de datos en tiempo real
- **SharpVectors.Wpf 1.8.4.2** - Renderizado de iconos SVG

## 🚀 Instalación y Uso

### Prerrequisitos
- .NET 8.0 Runtime (Windows)
- Visual Studio 2022 o superior (para desarrollo)

### Clonar el Repositorio
```bash
git clone https://github.com/tu-usuario/Planbie.git
cd Planbie
```

### Compilar y Ejecutar
```bash
dotnet build
dotnet run --project Planbie/Presentation
```

## 📁 Estructura del Proyecto

```
Planbie/
├── Planbie.sln                    # Solución de Visual Studio
└── Presentation/                  # Proyecto principal WPF
    ├── Presentation.csproj        # Configuración del proyecto
    ├── MainWindow.xaml            # Ventana principal
    ├── MainWindow.xaml.cs         # Lógica de la ventana
    ├── TimeAxisLabelConverter.cs  # Convertidor para etiquetas de tiempo
    └── Resources/                 # Recursos embebidos
        ├── *.svg                  # Iconos vectoriales
        └── *.ttf                  # Fuentes Outfit
```

## 🎨 Interfaz de Usuario

La aplicación presenta tres secciones principales:

1. **Panel de Temperatura**: Muestra la temperatura actual con un indicador circular progresivo
2. **Estado de la Planta**: Visualización del estado de salud mediante iconos SVG
3. **Gráfico Temporal**: Historial de temperatura con etiquetas de tiempo personalizadas

### Controles de Header
- Logo de la aplicación
- Botón de configuración de puertos
- Controles LED de estado
- Control de buzzer/alarma
- Botones de minimizar y cerrar

## 🔧 Características Técnicas

- **Convertidor de Tiempo Personalizado**: Transforma valores numéricos en etiquetas temporales legibles ("Ahora", "-1h", "-2h")
- **Recursos Embebidos**: Iconos SVG y fuentes integradas en el ejecutable
- **Tema Oscuro**: Interfaz con esquema de colores oscuro (#FF2B2B2B)

## 🤝 Contribuir

1. Fork el proyecto
2. Crea una rama para tu feature (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push a la rama (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

## 📄 Licencia

Este proyecto está bajo la Licencia MIT - ver el archivo [LICENSE](LICENSE) para más detalles.

---

⭐ ¡No olvides darle una estrella al proyecto si te resultó útil!
