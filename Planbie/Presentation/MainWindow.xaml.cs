using LiveCharts;
using LiveCharts.Wpf;
using Newtonsoft.Json.Linq;
using Presentation.Logica;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
namespace Presentation
{
    public partial class MainWindow : Window
    {
        private ChartValues<double> temperatureValues = new ChartValues<double>();
        
        private Arduino arduino;
        private Json json = new Json();
        
        private CancellationTokenSource cts;
        private int dataCounter = 0;

        public MainWindow()
        {
            InitializeComponent();

            CargarDatosJson(); // codigo para cargar y configurar el grafico de temperatura respecto al tiempo

        }

        #region cargar los registros de temperaturas del json en el grafico temp/tiempo y configurarlo
        private void CargarDatosJson()
        {

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

            foreach (TempData data in Json.LeerDatosJson())
            {
                AgregarTemperaturaGrafico(data);
            }
        }

        public void AgregarTemperaturaGrafico(TempData data)
        {
            temperatureValues.Add(data.Temperatura);

            var labels = temperatureChart.AxisX[0].Labels as List<string> ?? new List<string>();
            labels.Add(data.Fecha.ToString("HH:mm:ss"));
            temperatureChart.AxisX[0].Labels = labels;
        }
        #endregion


        private async void StartDataCollection()
        {
            if (EmergenteWindow.puertoSerial != null && EmergenteWindow.puertoSerial.IsOpen)
            {
                arduino = new Arduino(EmergenteWindow.puertoSerial);
                cts = new CancellationTokenSource();

                await Task.Run(async () =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            JObject data = await arduino.CollectData();
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

            // Actualizar la UI con los últimos valores
            //txtTemperatura.Text = $"Temperatura: {temperatura}°C";
            //txtHumedad.Text = $"Humedad: {data["humedad"]}%";
            //double distance = double.Parse(data["distancia"].ToString());
            //txtBoton.Text = $"Estado del botón: {(data["boton"].Value<int>() == 1 ? "Presionado" : "No presionado")}";

            if (data["boton"].ToString() == "REGANDO")
            {
                Debug.WriteLine("REGANDO -- boton");
                await arduino.Regando(1);
                elip_Azul.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF13D9FF"));
            }
            else
            {
                Debug.WriteLine("NO REGANDO -- boton");
                await arduino.Regando(0);
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
                await Dispatcher.InvokeAsync(async () => {
                    await Json.AgregarRegistroJson(tempData);
                });
                AgregarTemperaturaGrafico(tempData);
            }
            // Controla si la alerta de temperatura/humedad está activa
            // Controla si la alerta de distancia está activa
            

            if (humedad < 30 && temperatura > 35 )
            {
                // Si no está alertando la distancia, se ejecuta la alerta de temperatura/humedad
                await arduino.Alert_Temp();
                //await arduino.Regando();
                elip_Amarillo.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFD613"));
                //string newSvgPath = "/Resources/leaf-bad.svg";
                //img_planta.SetValue(SvgImage.SourceProperty, new Uri(newSvgPath, UriKind.Relative));
                
            }
            

            

        }

        private async void btnApagarLED_Click(object sender, RoutedEventArgs e)
        {
            if (arduino != null)
            {
                await arduino.TurnOffLED();
            }
        }

        private async void btnApagarBuzzer_Click(object sender, RoutedEventArgs e)
        {
            if (arduino != null)
            {
                await arduino.TurnOffBuzzer();
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

        private async Task registrar_temperatura(double temperatura)
        {
            var nuevoRegistro = new TempData
            {
                Fecha = DateTime.Now,
                Temperatura = temperatura
            };

            await Dispatcher.InvokeAsync(async () => {
                await Json.AgregarRegistroJson(nuevoRegistro);
            });

            AgregarTemperaturaGrafico(nuevoRegistro);
        }

        private void btn_add_Click(object sender, RoutedEventArgs e)
        {
            Random random = new Random();
            registrar_temperatura(random.Next(-5, 37));
        }

        private void btn_openWindowsPorts_Click(object sender, RoutedEventArgs e)
        {
            EmergenteWindow puertosWindow = new EmergenteWindow();
            puertosWindow.ShowDialog();

            if (EmergenteWindow.puertoSerial != null && EmergenteWindow.puertoSerial.IsOpen)
            {
                StartDataCollection();
            }
        }



    }
}