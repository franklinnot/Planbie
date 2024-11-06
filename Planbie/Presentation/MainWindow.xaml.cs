using LiveCharts;
using LiveCharts.Wpf;
using MQTT;
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
        private ChartValues<int> temperatureValues = new ChartValues<int>();
        private Arduino? arduino;
        private Json json = new Json();
        // util para cuando se quiera cancelar la tarea de recoleccion de datos o envio de comandos (metodo RecolectarDatos_Arduino)
        // public static por si en la ventana de ports cierra la conexion del puerto -- linea 90 en la ventana emergente y dentro btn_cerrar_Click en este doc
        public static CancellationTokenSource? cts;
        bool apagarBuzzer = false; // ayuda a verificar si se ha dado click en el boton del buzzer para que este este apagado o encendido
        bool alertaDetectada = false; // false para cuando no hay alertas y true para cuando si las hay
        bool buzzerEncendido = false; // una variable que ayuda a verificar si el buzzer esta encendido
        private System.Timers.Timer mqttTimer;
       
        public MainWindow()
        {
            InitializeComponent();
            CargaInicialDatosJson(); // codigo para cargar y configurar el grafico de temperatura respecto al tiempo
            //RecibirMQTT();
            //WindowLoad();
        }

        //Recibir 
        public async Task RecibirMQTT()
        {
            var mqttClient = new ConnectionMQTT();
            mqttClient.OnMessageReceived += (topic, message) =>
            {
                // Solamente se recepcionarán datos del topic: "telemetria"
                if (topic == "telemetria")
                {
                    Debug.WriteLine($"Datos de telemetría recibidos: {message}");
                }
            };

            // Conectar al cliente MQTT
            bool isConnected = await mqttClient.Connect();
            if (!isConnected) return;

            // Suscribirse a los tópicos
            await mqttClient.Subscribe("comandos");
            await mqttClient.Subscribe("telemetria");

            Debug.WriteLine("Publicando mensajes con datos simulados...");
        }

        // Enviar

        private void IniciarEnvioMQTT(ConnectionMQTT mqttClient)
        {
            mqttTimer = new System.Timers.Timer(5000); // Configura el temporizador para 5 segundos
            mqttTimer.Elapsed += async (sender, e) => await EnviarMQTT(mqttClient);
            mqttTimer.AutoReset = true; // Para que se repita automáticamente cada 5 segundos
            mqttTimer.Start();
        }

        private async Task EnviarMQTT(ConnectionMQTT mqttClient)
        {
            var rand = new Random();
            double temperature = 28.7;
            double humidity = 23;

            double temperaturaActual = temperature + rand.NextDouble();
            double humedadActual = humidity + rand.NextDouble();

            var payload = new
            {
                temperature = temperaturaActual,
                humidity = humedadActual
            };

            await mqttClient.Publish("comandos", payload);

            // Usa el Dispatcher de WPF para ejecutar en el hilo de la interfaz
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                Debug.WriteLine($"Datos enviados: Temperatura = {temperaturaActual}, Humedad = {humedadActual}");
            });
        }

        private async Task WindowLoad()
        {
            var mqttClient = new ConnectionMQTT();
            bool isConnected = await mqttClient.Connect();

            if (isConnected)
            {
                await mqttClient.Subscribe("comandos");
                await mqttClient.Subscribe("telemetria");

                IniciarEnvioMQTT(mqttClient); // Inicia el temporizador para el envío de datos cada 5 segundos
            }
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
                AgregarTemperaturaGrafico(data);
            }
        }

        public void AgregarTemperaturaGrafico(TempData data)
        {
            int temp = data.Temperatura;
            temperatureValues.Add(temp);

            var labels = temperatureChart.AxisX[0].Labels as List<string> ?? new List<string>();
            labels.Add(data.Fecha.ToString("g"));
            temperatureChart.AxisX[0].Labels = labels;
        }

        #endregion

        #region codigo para recolectar los datos segun su tipo de conexion

        // si se trabaja con conexion directa, se usara este metodo -- 2
        private async Task RecolectarDatos_Arduino()
        {
            if (EmergenteWindow.puertoSerial != null && EmergenteWindow.puertoSerial.IsOpen)
            {
                bool error_recopilacion = false;
                arduino = new Arduino(EmergenteWindow.puertoSerial); // instancia de la clase arduino solo si el puerto serial a sido abierto
                cts = new CancellationTokenSource(); // se inicializa el cts para recibir solicitudes de cancelacion

                // esto realiza una ejecucion en paralelo
                await Task.Run(async () =>
                {
                    // mientras no se haya solicitado la cancelación de la tarea
                    while (!cts.Token.IsCancellationRequested)
                    {
                        try
                        {
                            JObject data = await arduino.ObtenerDatos(); // json de los datos recibidos del ardu
                            Dispatcher.Invoke(() => ProcesarData(data)); // utiliza el dispatcher para actualizar la intefaz y no bloquear su hilo de ejcucion
                            await Task.Delay(5000, cts.Token); // se realizara la espera de 5 segundos y por el while volvera a ejecutarse
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            error_recopilacion = true;
                            MessageBox.Show("Hubo un error en la recopilación de datos");
                            Debug.WriteLine($"Error en la recopilación de datos: {ex.Message}");
                            cts?.Cancel();
                            arduino = null;
                            EmergenteWindow.puertoSerial.Close();
                        }
                    }
                }, cts.Token); // se pasa el token de cancelación a Task.Run 

                if (!error_recopilacion) {
                    // cuando la tarea de recoleccion de datos termine
                    MessageBox.Show("La recolección de datos del Arduino ha culminado.");                
                }
            }
            else
            {
                MessageBox.Show("El puerto serial no está abierto. Por favor, conéctese primero.");
            }
        }

        #endregion

        #region Codigo para procesar la data recibida, registrar en el json, actualizar la interfaz y enviar comandos segun el tipo de conexion

        // metodo util para procesar la data recibida, sin importar el tipo de conexion
        private async void ProcesarData(JObject data)
        {
            try
            {
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
                await RegistrarTemperaturaJson(dataTemporal);
                await NotificarAlertas(temperatura, humedad, estado_boton);

                ActualizarInterfaz(dataTemporal, humedad);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Ha sucedido un error al procesar los datos: {e.Message}");
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
                elip_temperatura.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF1349"));
            }
            else if (temperatura >= 30 && temperatura < 40)
            {
                elip_temperatura.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFD613"));
            }
            else
            {
                elip_temperatura.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00ff77"));
            }

            // led de humedad
            if (humedad >= 70)
            {
                elip_temperatura.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF00ff77"));
            }
            else if (temperatura >= 60 && temperatura < 70)
            {
                elip_temperatura.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFD613"));
            }
            else
            {
                elip_temperatura.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF1349"));
            }

            // notificamos la alerta
            if (temperatura >= 40 && humedad < 60)
            {
                Debug.WriteLine("ALERTA: Alta temperatura y baja humedad.");
                alertaDetectada = true;
                await arduino.EstadoPeligro(true);
                if (!apagarBuzzer) { // si el buzzer no ha sido apagado
                    await EstadoBuzzer(true);
                }
            }
            else
            {
                alertaDetectada = false;
                await arduino.EstadoPeligro(false);
                // solo si la alerta no ha sido enviada, se evaluara el estado del boton manual de riego
                await EstadoBotonRiego(estado_boton);
                await EstadoBuzzer(false);
            }
        }

        // metodo para enviarle el comando de regar utilizando la instancia de la clase arduino
        // y cambiar de color al boton correspondiente si se presiona el push button
        // solo se activara si no hay una alerta ejecutandose
        private async Task EstadoBotonRiego (string botonEstado)
        {
            if (arduino != null) { 
                // agregar codigo para verificar con que tipo de conexion se desea trabajar
                if (botonEstado == "PUSH_ON")
                {
                    Debug.WriteLine("REGANDO - POR BOTON");
                    await arduino.Regar(true);
                    elip_Azul.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF13D9FF"));
                }
                else
                {
                    Debug.WriteLine("NO REGANDO - POR BOTON");
                    await arduino.Regar(false);
                    elip_Azul.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF808080"));
                }
            }
            
        }

        private async Task EstadoBuzzer(bool encender)
        {
            if (encender && !buzzerEncendido)
            {
                await arduino.EstadoBuzzer(true);
                buzzerEncendido = true;
            }
            else if (!encender && buzzerEncendido)
            {
                await arduino.EstadoBuzzer(false);
                buzzerEncendido = false;
            }
        }

        // metodo para actualizar los controles de la interfaz en base a la data recibida
        private void ActualizarInterfaz(TempData temp, int humedad)
        {
            control_humedad.Value = humedad;
            control_temperatura.Value = temp.Temperatura;
            control_temperatura.Text = $"{temp.Temperatura}°C";

            AgregarTemperaturaGrafico(temp);
        }

        #endregion

        #region Eventos

        // cerrar conexiones
        private async Task CerrarConexion()
        {


        }

        // evento click para abrir la ventana de tipo de conexion
        private void btn_seleccionarConexion_Click(object sender, RoutedEventArgs e)
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
                panel_btn_puertos.Visibility = Visibility.Hidden;
                panel_btn_mqtt.Visibility = Visibility.Hidden;
            }
        }

        // evento click para abrir la ventana de conexion de puertos 
        private async void btn_openWindowPorts_Click(object sender, RoutedEventArgs e)
        {
            EmergenteWindow puertosWindow = new EmergenteWindow();
            puertosWindow.ShowDialog();

            // verifica si luego de cerrar la ventana de los puertos, se conecto a uno de ellos
            if (EmergenteWindow.puertoSerial != null && EmergenteWindow.puertoSerial.IsOpen)
            {
                await RecolectarDatos_Arduino();
            }

        }

        // evento click para abrir la ventana de conectarse al broker
        private async void btn_openWindowMQTT_Click(object sender, RoutedEventArgs e)
        {
            BrokerCluster ventanaMQTT = new BrokerCluster();
            ventanaMQTT.ShowDialog();

            // verifica si luego de cerrar la ventana de los puertos, se conecto a uno de ellos
            //if (EmergenteWindow.puertoSerial != null && EmergenteWindow.puertoSerial.IsOpen)
            //{
            //    //await RecolectarDatos_Arduino();
            //}
        }

        // evento click para apagar el buzzer
        private async void btnApagarBuzzer_Click(object sender, RoutedEventArgs e)
        {
            if (arduino != null)
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
        private void btn_cerrar_Click(object sender, RoutedEventArgs e)
        {
            cts?.Cancel();
            if (EmergenteWindow.puertoSerial != null && EmergenteWindow.puertoSerial.IsOpen)
            {
                EmergenteWindow.puertoSerial.Close();
            }
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