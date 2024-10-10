using LiveCharts.Defaults;
using LiveCharts;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LiveCharts.Wpf;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Text.Json;

namespace Presentation
{
    public partial class MainWindow : Window
    {
        private ChartValues<double> temperatureValues;
        private const string archivoJson = "temperatures.json";

        public MainWindow()
        {
            InitializeComponent();

            temperatureValues = new ChartValues<double>();

            // Configura el gráfico con una serie de líneas
            temperatureChart.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Temperatura",
                    Values = temperatureValues
                }
            };

            // Inicializa el eje X si es necesario
            if (temperatureChart.AxisX.Count == 0)
            {
                temperatureChart.AxisX.Add(new Axis
                {
                    Labels = new List<string>()
                });
            }

            // Carga los datos iniciales desde el archivo JSON
            CargarDatosIniciales();


            // Registros de prueba
            //registrar_temperatura(12);
            //registrar_temperatura(18);
            //registrar_temperatura(24);
        }

        #region Mostrar la tempratura respecto al tiempo
        private void CargarDatosIniciales()
        {
            // Lee los datos y agrega al gráfico
            foreach (TempData data in LeerDatos())
            {
                AddTemperatureReading(data);
            }
        }

        public static List<TempData> LeerDatos()
        {
            // Verifica si el archivo existe antes de leer
            if (!File.Exists(archivoJson))
            {
                return new List<TempData>(); // Si no existe, devuelve una lista vacía
            }

            string json = File.ReadAllText(archivoJson);
            return JsonConvert.DeserializeObject<List<TempData>>(json);
        }

        public static void AgregarRegistro(TempData nuevoRegistro)
        {
            List<TempData> registros = LeerDatos();
            registros.Add(nuevoRegistro);
            GuardarDatos(registros);

            Debug.WriteLine(JsonConvert.SerializeObject(registros, Formatting.Indented)); // Solo para depuración
        }

        public static void GuardarDatos(List<TempData> registros)
        {
            string json = JsonConvert.SerializeObject(registros, Formatting.Indented);
            File.WriteAllText(archivoJson, json);
        }

        // Método para agregar una nueva lectura de temperatura
        public void AddTemperatureReading(TempData data)
        {
            // Agrega la nueva temperatura
            temperatureValues.Add(data.Temperatura);

            // Obtiene las etiquetas del eje X
            var labels = temperatureChart.AxisX[0].Labels as List<string> ?? new List<string>();

            // Agrega la nueva etiqueta de tiempo
            labels.Add(data.Fecha.ToString("HH:mm:ss"));

            // Actualiza las etiquetas del eje X
            temperatureChart.AxisX[0].Labels = labels;
        }
        #endregion

        // Método para registrar una nueva temperatura
        private void registrar_temperatura(double temperatura)
        {
            // Crear un nuevo registro de temperatura
            var nuevoRegistro = new TempData
            {
                Fecha = DateTime.Now,
                Temperatura = temperatura
            };

            // Agregar el nuevo registro
            AgregarRegistro(nuevoRegistro);

            // Actualizar el gráfico con la nueva temperatura
            AddTemperatureReading(nuevoRegistro);
        }

        private void btn_add_Click(object sender, RoutedEventArgs e)
        {
            Random random = new Random();
            registrar_temperatura(random.Next(-5, 37));
        }

        private void btn_minimizar_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btn_cerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void panel_header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Verificar si el botón izquierdo fue presionado
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                // Mover la ventana
                this.DragMove();
            }
        }
    }

}
