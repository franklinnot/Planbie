using LiveCharts.Defaults;
using LiveCharts;
using System;
using System.Windows;
using System.Windows.Input;
using LiveCharts.Wpf;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics.Tracing;
using System.Windows.Media;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;
using SharpVectors.Renderers;
using System;
using Presentation.Logica;
namespace Presentation
{
    public partial class MainWindow : Window
    {
        private ChartValues<double> temperatureValues;
        private const string archivoJson = "temperatures.json";
        private ArduinoInteraction arduinoInteraction;
        private CancellationTokenSource cts;
        private int dataCounter = 0;

        public MainWindow()
        {
            InitializeComponent();
            elip_Azul.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF808080"));
            elip_Amarillo.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF808080"));
            elip_Rojo.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF808080"));
            temperatureValues = new ChartValues<double>();

            temperatureChart.Series = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Temperatura",
                    Values = temperatureValues
                }
            };

            if (temperatureChart.AxisX.Count == 0)
            {
                temperatureChart.AxisX.Add(new Axis
                {
                    Labels = new List<string>()
                });
            }

            CargarDatosIniciales();
        }

        private void CargarDatosIniciales()
        {
            foreach (TempData data in LeerDatos())
            {
                AddTemperatureReading(data);
            }
        }

        public static List<TempData> LeerDatos()
        {
            if (!File.Exists(archivoJson))
            {
                return new List<TempData>();
            }

            string json = File.ReadAllText(archivoJson);
            return JsonConvert.DeserializeObject<List<TempData>>(json);
        }

        public static void AgregarRegistro(TempData nuevoRegistro)
        {
            List<TempData> registros = LeerDatos();
            registros.Add(nuevoRegistro);
            GuardarDatos(registros);

            Debug.WriteLine(JsonConvert.SerializeObject(registros, Formatting.Indented));
        }

        public static void GuardarDatos(List<TempData> registros)
        {
            string json = JsonConvert.SerializeObject(registros, Formatting.Indented);
            File.WriteAllText(archivoJson, json);
        }

        public void AddTemperatureReading(TempData data)
        {
            temperatureValues.Add(data.Temperatura);

            var labels = temperatureChart.AxisX[0].Labels as List<string> ?? new List<string>();
            labels.Add(data.Fecha.ToString("HH:mm:ss"));
            temperatureChart.AxisX[0].Labels = labels;
        }

        private async void StartDataCollection()
        {
            if (EmergenteWindow.puertoSerial != null && EmergenteWindow.puertoSerial.IsOpen)
            {
                arduinoInteraction = new ArduinoInteraction(EmergenteWindow.puertoSerial);
                cts = new CancellationTokenSource();

                await Task.Run(async () =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            JObject data = await arduinoInteraction.CollectData();
                            Dispatcher.Invoke(() => OnDataReceived(data));
                            await Task.Delay(1000, cts.Token); // Espera 1 segundo antes de la próxima lectura
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error collecting data: {ex.Message}");
                        }
                    }
                }, cts.Token);
            }
            else
            {
                MessageBox.Show("El puerto serial no está abierto. Por favor, conéctese primero.");
            }
        }

        private async void OnDataReceived(JObject data)
        {
            int temperatura = data["temperatura"].Value<int>();

            var tempData = new TempData
            {
                Fecha = DateTime.Now,
                Temperatura = temperatura
            };

            AddTemperatureReading(tempData);

            // Actualizar la UI con los últimos valores
            //txtTemperatura.Text = $"Temperatura: {temperatura}°C";
            //txtHumedad.Text = $"Humedad: {data["humedad"]}%";
            //double distance = double.Parse(data["distancia"].ToString());
            //txtBoton.Text = $"Estado del botón: {(data["boton"].Value<int>() == 1 ? "Presionado" : "No presionado")}";

            if (data["boton"].ToString() == "REGANDO")
            {
                Debug.WriteLine("REGANDO -- boton");
                await arduinoInteraction.Regando(1);
                elip_Azul.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF13D9FF"));
            }
            else
            {
                Debug.WriteLine("NO REGANDO -- boton");
                await arduinoInteraction.Regando(0);
                elip_Azul.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF808080"));
            }


            double humedad = double.Parse((data["humedad"]).ToString());
            mostrar_humedad.Value = humedad;
            mostrar_temperatura.Value = temperatura;
            mostrar_temperatura.Text = $"{temperatura.ToString()}°C";
            // Guardar datos cada 3 segundos
            dataCounter++;
            if (dataCounter % 3 == 0)
            {
                Task.Run(() => AgregarRegistro(tempData));
            }
            // Controla si la alerta de temperatura/humedad está activa
            // Controla si la alerta de distancia está activa
            

            if (humedad < 30 && temperatura > 35 )
            {
                // Si no está alertando la distancia, se ejecuta la alerta de temperatura/humedad
                await arduinoInteraction.Alert_Temp();
                //await arduinoInteraction.Regando();
                elip_Amarillo.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFD613"));
                //string newSvgPath = "/Resources/leaf-bad.svg";
                //img_planta.SetValue(SvgImage.SourceProperty, new Uri(newSvgPath, UriKind.Relative));
                
            }
            

            

        }

        private async void btnApagarLED_Click(object sender, RoutedEventArgs e)
        {
            if (arduinoInteraction != null)
            {
                await arduinoInteraction.TurnOffLED();
            }
        }

        private async void btnApagarBuzzer_Click(object sender, RoutedEventArgs e)
        {
            if (arduinoInteraction != null)
            {
                await arduinoInteraction.TurnOffBuzzer();
                MessageBox.Show("Apagué tu feo Bauser");
            }
        }

        private void btn_minimizar_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void btn_cerrar_Click(object sender, RoutedEventArgs e)
        {
            cts?.Cancel();
            this.Close();
        }

        private void panel_header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void AbrirVentanaPuertos(object sender, RoutedEventArgs e)
        {
            EmergenteWindow puertosWindow = new EmergenteWindow();
            puertosWindow.ShowDialog();

            if (EmergenteWindow.puertoSerial != null && EmergenteWindow.puertoSerial.IsOpen)
            {
                StartDataCollection();
            }
        }

        private void registrar_temperatura(double temperatura)
        {
            var nuevoRegistro = new TempData
            {
                Fecha = DateTime.Now,
                Temperatura = temperatura
            };

            AgregarRegistro(nuevoRegistro);
            AddTemperatureReading(nuevoRegistro);
        }

        private void btn_add_Click(object sender, RoutedEventArgs e)
        {
            Random random = new Random();
            registrar_temperatura(random.Next(-5, 37));
        }

        private void led_Rojo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void led_Amarillo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void led_Azul_Click(object sender, RoutedEventArgs e)
        {

        }
    }

}