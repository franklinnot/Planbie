using LiveCharts;
using LiveCharts.Wpf;
using MQTT;
using Newtonsoft.Json.Linq;
using Presentation.Logica;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
namespace Presentation
{
    public partial class MainWindow : Window
    {
        private ChartValues<int> temperatureValues = new ChartValues<int>();
        private Json json = new Json();
        // util para cuando se quiera cancelar la tarea de recoleccion de datos o envio de comandos (metodo RecolectarDatos_Arduino)
        // public static por si en la ventana de ports cierra la conexion del puerto -- linea 90 en la ventana emergente y dentro btn_cerrar_Click en este doc
        public static CancellationTokenSource? cts;
        bool apagarBuzzer = false; // ayuda a verificar si se ha dado click en el boton del buzzer para que este este apagado o encendido
        bool alertaDetectada = false; // false para cuando no hay alertas y true para cuando si las hay
        bool buzzerEncendido = false; // una variable que ayuda a verificar si el buzzer esta encendido
        private System.Timers.Timer mqttTimer;
        private bool disposed = false;

        public MainWindow()
        {
            InitializeComponent();

            CargaInicialDatosJson(); // codigo para cargar y configurar el grafico de temperatura respecto al tiempo
        }

        #region cargar los registros de temperaturas del json en el grafico temp/tiempo y configurarlo
        private async void CargaInicialDatosJson()
        {
            // Configuracion inicial del grafico
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

            List<TempData> registros = await Json.LeerDatosJson();
            // cargar los valores iniciales del grafico
            foreach (TempData data in registros)
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    await AgregarTemperaturaGrafico(data);
                    
                });
            }

            await Dispatcher.InvokeAsync(async () =>
            {
                await ActualizarPromedioTemperatura();

            });
            

        }

        public async Task AgregarTemperaturaGrafico(TempData data)
        {
            int temp = data.Temperatura;
            temperatureValues.Add(temp);

            var labels = temperatureChart.AxisX[0].Labels as List<string> ?? new List<string>();
            labels.Add(data.Fecha.ToString("g"));
            temperatureChart.AxisX[0].Labels = labels;



        }

        public static async Task<int> CalcularPromedioTemperatura(List<TempData> datos)
        {
            // Verifica que la lista no esté vacía para evitar errores al calcular el promedio
            if (datos == null || datos.Count == 0)
            {
                return 0; // Retorna 0 si no hay datos
            }

            // Calcula el promedio de la propiedad Temperatura y redondea al entero más cercano
            int promedio = (int)Math.Round(datos.Average(d => d.Temperatura));

            return promedio;
        }


        private async Task ActualizarPromedioTemperatura() {
            List<TempData> registros = await Json.LeerDatosJson();
            await Dispatcher.InvokeAsync(async () =>
            {
                promedio_temperatura.Text = $"{await CalcularPromedioTemperatura(registros)}°C";
            });
        }

        #endregion

        #region codigo para recolectar los datos segun su tipo de conexion

        // si se trabaja con conexion directa, se usara este metodo 
        private async Task RecolectarDatos_Arduino()
        {
            cts = new CancellationTokenSource();
            ArduinoControl.Instancia.OnDataReceived += ProcesarData;

            try
            {
                await ArduinoControl.Instancia.ObtenerDatos(cts.Token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en la recopilación de datos: {ex.Message}");
                await ManejarErrorConexion(ex);
            }
            
        }

        // si se trabaja con un broker mqtt, se usara este metodo
        private async Task RecolectarDatos_MQTT()
        {
            cts = new CancellationTokenSource(); // se inicializa el cts para recibir solicitudes de cancelacion;
            ConnectionMQTT.Instancia.OnDataReceived += ProcesarData;

            try
            {
                await ConnectionMQTT.Instancia.ObtenerDatos(cts.Token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error en la recopilación de datos: {ex.Message}");
                await ManejarErrorConexion(ex);
            }
        }

        // metodo para manejar los errores en la conexion de ambos tipos
        private async Task ManejarErrorConexion(Exception ex)
        {
            if (SeleccionWorkspace.workspace == "ARDUINO")
            {
                try
                {
                    // si la conexion no ha sido cerrada MANUALMENTE
                    if (!EmergenteWindow.conexionCerrada)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            MessageBox.Show($"Ha sucedido un error al establecer comunicación con el dispositivo: {ex.Message}");
                            MessageBox.Show("La recolección de datos del ArduinoControl ha culminado.");
                        });
                    }

                    if (cts != null)
                    {
                        cts?.Cancel();
                    }
                    await Task.Run(() => ArduinoControl.Instancia.Disconnect());

                    await Dispatcher.InvokeAsync(async () =>
                    {
                        await InterfazDesconetada();
                    });
                }
                catch (Exception innerEx)
                {
                    Debug.WriteLine($"Error al manejar la desconexión: {innerEx.Message}");
                }
            }
            else if (SeleccionWorkspace.workspace == "MQTT") {
                try
                {
                    // si la conexion no ha sido cerrada MANUALMENTE
                    if (!BrokerCluster.conexionCerrada)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            MessageBox.Show($"Ha sucedido un error al establecer comunicación con el broker: {ex.Message}");
                            MessageBox.Show("La recolección de datos del Broker ha culminado.");
                        });
                    }

                    if (cts != null)
                    {
                        cts?.Cancel();
                    }
                    await Task.Run(() => ConnectionMQTT.Instancia.Disconnect());

                    await Dispatcher.InvokeAsync(async () =>
                    {
                        await InterfazDesconetada();
                    });
                }
                catch (Exception innerEx)
                {
                    Debug.WriteLine($"Error al manejar la desconexión: {innerEx.Message}");
                }
            }
        }

        #endregion

        #region Codigo para procesar la data recibida, registrar en el json, actualizar la interfaz y enviar comandos segun el tipo de conexion

        // metodo util para procesar la data recibida, sin importar el tipo de conexion
        private async void ProcesarData(string datos_planos)
        {
            Debug.WriteLine(datos_planos);
            try
            {
                JObject data = JObject.Parse(datos_planos); // parseo de la data recibida como un json
                int temperatura = data["temperatura"].Value<int>();
                int humedad = data["humedad"].Value<int>();
                string estado_boton = data["boton"].ToString();
                // variable temporal para almacenar los datos  de la temperatura

                var dataTemporal = new TempData
                {
                    Fecha = DateTime.Now,
                    Temperatura = temperatura
                };

                // metodos asincronos por el hecho de tener que recibir datos desde un dispositivo externo
                // y tambien por modificar el archivo json
                await Dispatcher.InvokeAsync(async () =>
                {
                    await RegistrarTemperaturaJson(dataTemporal);
                    await ActualizarInterfaz(dataTemporal, humedad);
                    await ActualizarPromedioTemperatura();
                    await NotificarAlertas(temperatura, humedad, estado_boton);
                });

            }
            catch
            {
                cts?.Cancel();
                if (SeleccionWorkspace.workspace == "ARDUINO" && ArduinoControl.Instancia.IsConnected) {
                    ArduinoControl.Instancia.Disconnect();
                }
                else if (SeleccionWorkspace.workspace == "MQTT" && ConnectionMQTT.Instancia.IsConnected) {
                    await ConnectionMQTT.Instancia.Disconnect();
                }
                await Dispatcher.InvokeAsync(async () =>
                {
                    await InterfazDesconetada();
                });
                
            }
        }

        // metodo para registrar la temperatura para el json usando instancia de esa clase
        private async Task RegistrarTemperaturaJson(TempData nuevoRegistro)
        {
            await Dispatcher.InvokeAsync(async () => {
                await Json.RegistrarTemperaturaJson(nuevoRegistro);
            });
        }

        // metodo para evaluar los datos recibidos y tomar decisiones en basea a sus valores
        private async Task NotificarAlertas(int temperatura, int humedad, string estado_boton)
        {
            // agregar codigo para verificar con que tipo de conexion se desea trabajar

            /*
            color verde: #FF00ff77
            color rojo: #FFFF1349
            color amarillo: #FFFFD613
            */

            // led de temperatura


                if (temperatura >= 40)
                {
                    elip_temperatura.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF1349")); // rojo
                }
                else if (temperatura >= 30 && temperatura < 40)
                {
                    elip_temperatura.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFD613")); // amarillo
                }
                else
                {
                    elip_temperatura.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00ff77")); // verde
                }

                // led de humedad
                if (humedad >= 70)
                {
                    elip_humedad.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00ff77")); // verde
                }
                else if (humedad >= 60 && humedad < 70)
                {
                    elip_humedad.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFD613")); // amarillo
                }
                else
                {
                    elip_humedad.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF1349")); // rojo
                }

            if (SeleccionWorkspace.workspace == "ARDUINO" && ArduinoControl.Instancia.IsConnected)
            {
                // notificamos la alerta
                if (temperatura >= 40 && humedad < 60)
                {
                    Debug.WriteLine("ALERTA: Alta temperatura y baja humedad.");
                    elip_riego.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF13D9FF"));
                    alertaDetectada = true;
                    await ArduinoControl.Instancia.EstadoPeligro(true);
                    if (!apagarBuzzer)
                    { // si el buzzer no ha sido apagado
                        await EstadoBuzzer(true);
                    }
                    else
                    { // si el buzzer ha sido apagado
                        await EstadoBuzzer(false);
                    }
                }
                else
                {
                    elip_riego.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF808080"));
                    alertaDetectada = false;
                    await ArduinoControl.Instancia.EstadoPeligro(false);
                    // solo si la alerta no ha sido enviada, se evaluara el estado del boton manual de riego
                    await EstadoBotonRiego(estado_boton);
                    await EstadoBuzzer(false);
                }
            }
            else if (SeleccionWorkspace.workspace == "MQTT" && ConnectionMQTT.Instancia.IsConnected)
            {
                // notificamos la alerta
                if (temperatura >= 40 && humedad < 60)
                {
                    Debug.WriteLine("ALERTA: Alta temperatura y baja humedad.");
                    elip_riego.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF13D9FF")); // celeste
                    alertaDetectada = true;
                    await ConnectionMQTT.Instancia.EstadoPeligro(true);
                    if (!apagarBuzzer)
                    { // si el buzzer no ha sido apagado
                        await EstadoBuzzer(true);
                    }
                    else
                    { // si el buzzer ha sido apagado
                        await EstadoBuzzer(false);
                    }
                }
                else
                {
                    elip_riego.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF808080")); // gris
                    alertaDetectada = false;
                    await ConnectionMQTT.Instancia.EstadoPeligro(false);
                    // solo si la alerta no ha sido enviada, se evaluara el estado del boton manual de riego
                    await EstadoBotonRiego(estado_boton);
                    await EstadoBuzzer(false);
                }
            }
        }

        // metodo para enviarle el comando de regar utilizando la instancia de la clase arduino
        // y cambiar de color al boton correspondiente si se presiona el push button
        // solo se activara si no hay una alerta ejecutandose
        private async Task EstadoBotonRiego (string botonEstado)
        {
            if (ArduinoControl.Instancia.IsConnected) { 
                // agregar codigo para verificar con que tipo de conexion se desea trabajar
                if (botonEstado == "REGAR_ON")
                {
                    Debug.WriteLine("BOTON PRESIONADO - REGANDO");
                    await ArduinoControl.Instancia.Regar(true);
                    if (!apagarBuzzer)
                    { // si el buzzer no ha sido apagado
                        await EstadoBuzzer(true);
                    }
                    else
                    { // si el buzzer ha sido apagado
                        await EstadoBuzzer(false);
                    }
                    elip_riego.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF13D9FF"));
                }
                else
                {
                    await ArduinoControl.Instancia.Regar(false);
                    elip_riego.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF808080"));
                }
            }
            else if (ConnectionMQTT.Instancia.IsConnected)
            {
                // agregar codigo para verificar con que tipo de conexion se desea trabajar
                if (botonEstado == "REGAR_ON")
                {
                    Debug.WriteLine("BOTON PRESIONADO - REGANDO");
                    await ConnectionMQTT.Instancia.Regar(true);
                    if (!apagarBuzzer)
                    { // si el buzzer no ha sido apagado
                        await EstadoBuzzer(true);
                    }
                    else
                    { // si el buzzer ha sido apagado
                        await EstadoBuzzer(false);
                    }
                    elip_riego.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF13D9FF"));
                }
                else
                {
                    await ArduinoControl.Instancia.Regar(false);
                    elip_riego.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF808080"));
                }
            }
            
        }

        private async Task EstadoBuzzer(bool encender)
        {
            if (SeleccionWorkspace.workspace == "ARDUINO" && ArduinoControl.Instancia.IsConnected) {
                if (encender && !buzzerEncendido)
                {
                    await ArduinoControl.Instancia.EstadoBuzzer(true);
                    buzzerEncendido = true;
                }
                else if (!encender && buzzerEncendido)
                {
                    await ArduinoControl.Instancia.EstadoBuzzer(false);
                    buzzerEncendido = false;
                }
            }
            else if (SeleccionWorkspace.workspace == "MQTT" && ConnectionMQTT.Instancia.IsConnected) {
                if (encender && !buzzerEncendido)
                {
                    await ConnectionMQTT.Instancia.EstadoBuzzer(true);
                    buzzerEncendido = true;
                }
                else if (!encender && buzzerEncendido)
                {
                    await ConnectionMQTT.Instancia.EstadoBuzzer(false);
                    buzzerEncendido = false;
                }
            }
        }

        // metodo para actualizar los controles de la interfaz en base a la data recibida
        private async Task ActualizarInterfaz(TempData temp, int humedad)
        {
            try
            {
                // Actualizar el valor de la humedad
                control_humedad.Value = humedad;
                control_temperatura.Value = temp.Temperatura;
                control_temperatura.Text = $"{temp.Temperatura}°C";
                await AgregarTemperaturaGrafico(temp);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }


        public async Task InterfazDesconetada()
        {
            try
            {
                // Cambia el color de cada elipse en el hilo de la UI
                await Dispatcher.InvokeAsync(() =>
                {
                    elip_temperatura.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF808080"));
                });

                await Dispatcher.InvokeAsync(() =>
                {
                    elip_humedad.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF808080"));
                });

                await Dispatcher.InvokeAsync(() =>
                {
                    elip_riego.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF808080"));
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        #endregion

        #region Metodos para liberar memoria
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Liberar recursos administrados
                    cts?.Dispose();
                    mqttTimer?.Dispose();

                    // Desuscribir eventos
                    if (ArduinoControl.Instancia != null)
                    {
                        ArduinoControl.Instancia.OnDataReceived -= ProcesarData;
                    }
                    if (ConnectionMQTT.Instancia != null)
                    {
                        ConnectionMQTT.Instancia.OnDataReceived -= ProcesarData;
                    }
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Modificar el método de cierre
        private async Task CerrarAplicacion()
        {
            try
            {
                if (cts != null)
                {
                    await Task.Run(() => cts.Cancel());
                }

                if (ArduinoControl.Instancia?.IsConnected == true)
                {
                    await Task.Run(() => ArduinoControl.Instancia.Disconnect());
                }

                Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error al cerrar la aplicación: {ex.Message}");
            }
        }
        #endregion

        #region Eventos

        // evento click para abrir la ventana de tipo de conexion
        private async void btn_seleccionarConexion_Click(object sender, RoutedEventArgs e)
        {
            SeleccionWorkspace tipoConexion = new SeleccionWorkspace();
            tipoConexion.ShowDialog();

            // verificamos su decision luego de cerrar la ventana
            if (!string.IsNullOrEmpty(SeleccionWorkspace.workspace)) {
                // si se selecciono una conexion, se habilitaran los botones para que pueda conectarse
                string worspace = SeleccionWorkspace.workspace;
                if (worspace == "ARDUINO")
                {
                    panel_btn_mqtt.Visibility = Visibility.Hidden;
                    panel_btn_puertos.Visibility = Visibility.Visible;
                }
                else if (worspace == "MQTT")
                {
                    panel_btn_puertos.Visibility = Visibility.Hidden;
                    panel_btn_mqtt.Visibility = Visibility.Visible;
                }
            }
            else
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    await InterfazDesconetada();
                });
                panel_btn_puertos.Visibility = Visibility.Hidden;
                panel_btn_mqtt.Visibility = Visibility.Hidden;
            }
        }

        // evento click para abrir la ventana de conexion de puertos 
        private async void btn_openWindowPorts_Click(object sender, RoutedEventArgs e)
        {
            EmergenteWindow puertosWindow = new EmergenteWindow();
            puertosWindow.ShowDialog();

            // verifica si se conecto a un puerto luego de cerrar la ventana de conexion
            if (ArduinoControl.Instancia.IsConnected)
            {
                await RecolectarDatos_Arduino();
            }
            else
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    await InterfazDesconetada();
                });
            }

        }

        // evento click para abrir la ventana de conectarse al broker
        private async void btn_openWindowMQTT_Click(object sender, RoutedEventArgs e)
        {
            BrokerCluster ventanaMQTT = new BrokerCluster();
            ventanaMQTT.ShowDialog();

            // verifica si luego de cerrar la ventana de los puertos, se conecto a uno de ellos
            if (ConnectionMQTT.Instancia.IsConnected)
            {
                await RecolectarDatos_MQTT();
            }
            else
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    await InterfazDesconetada();
                });
            }
            // agregar codigo para verificar si se desconecto dandole click al boton, si es aso llamar al metodo de interfaz desconectada con el dispatcher
        }

        // evento click para apagar el buzzer
        private async void btnApagarBuzzer_Click(object sender, RoutedEventArgs e)
        {
            if (SeleccionWorkspace.workspace == "ARDUINO" && ArduinoControl.Instancia.IsConnected)
            {
                apagarBuzzer = !apagarBuzzer; // cada que haga click, se alterna su estadop

                if (apagarBuzzer)
                {
                    // si ha sido detectada una alerta significa que le buzzer esta prendido
                    // por lo tanto, al darle click a este boton, lo apagara
                    Debug.WriteLine("Buzzer apagado manualmente");
                    await EstadoBuzzer(false);
                    MessageBox.Show("El buzzer permanecerá apagado.");
                }
                else
                {
                    Debug.WriteLine("Buzzer encendido manualmente");
                    // si se vuelve a activar el buzzer manualmente, respeta las alertas actuales
                    if (alertaDetectada)
                    {
                        await EstadoBuzzer(true);
                    }
                }
            }
            else if (SeleccionWorkspace.workspace == "MQTT" && ConnectionMQTT.Instancia.IsConnected)
            {
                apagarBuzzer = !apagarBuzzer; // cada que haga click, se alterna su estadop

                if (apagarBuzzer)
                {
                    // si ha sido detectada una alerta significa que le buzzer esta prendido
                    // por lo tanto, al darle click a este boton, lo apagara
                    Debug.WriteLine("Buzzer apagado manualmente");
                    await EstadoBuzzer(false);
                    MessageBox.Show("El buzzer permanecerá apagado.");
                }
                else
                {
                    Debug.WriteLine("Buzzer encendido manualmente");
                    // si se vuelve a activar el buzzer manualmente, respeta las alertas actuales
                    if (alertaDetectada)
                    {
                        await EstadoBuzzer(true);
                    }
                }
            }
        }

        // evento click para culminar la tarea de recolectar datos, cerrar el puerto serial y cerrar la ventana principal,
        private async void btn_cerrar_Click(object sender, RoutedEventArgs e)
        {
            await CerrarAplicacion();
            this.Close();
        }

        #region Eventos solo para la interaccion con la intefaz, sin logica

        private void btn_minimizar_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void panel_header_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }


        #endregion

        #endregion

    }
}