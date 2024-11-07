using HiveMQtt.Client;
using HiveMQtt.Client.Options;
using HiveMQtt.MQTT5.ReasonCodes;
using HiveMQtt.MQTT5.Types;
using System.Diagnostics;
using System.Text.Json;
using Newtonsoft.Json.Linq;
namespace MQTT
{
    public class ConnectionMQTT
    {
        private HiveMQClient client;
        private HiveMQClientOptions options;
        public string TopicTelemetria { get; set; }
        public string TopicComandos { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool IsConnected => client?.IsConnected() ?? false;
        // evento para cuando se recibe un mensaje, se incluye tambien el topic de origen
        public event Action<string>? OnDataReceived; // (topic, message)
        // public event Action<string, string>? OnDataReceived; // esto ya no pues se trabajar con un topic en especifico (telemetria)

        // singleton -- instancia única
        private static readonly ConnectionMQTT _instancia = new ConnectionMQTT();
        public static ConnectionMQTT Instancia => _instancia;

        private ConnectionMQTT()
        {
            Debug.WriteLine("Se creó una instancia de ConnectionMQTT");
        }

        // metodo para configurar el cliente con lo parametros dados por el usuario
        public void Inicializar(string host, int port, string username, string password)
        {
            if (client != null && client.IsConnected())
            {
                Debug.WriteLine("El cliente ya está inicializado y conectado.");
                return;
            }

            options = new HiveMQClientOptions
            {
                Host = host,
                Port = port,
                UseTLS = true,
                UserName = username,
                Password = password
            };

            Host = host;
            Port = port;
            Username = username;
            Password = password;

            client = new HiveMQClient(options);
            client.OnMessageReceived += (sender, args) =>
            {
                string mensajeRecibido = args.PublishMessage.PayloadAsString;
                string? topic = args.PublishMessage.Topic ?? "Topic desconocido";
                //OnDataReceived?.Invoke(topic, mensajeRecibido);
                if (topic == TopicTelemetria)
                {
                    OnDataReceived?.Invoke(mensajeRecibido);
                }
            };

            Debug.WriteLine("Cliente MQTT inicializado.");
        }

        // metodo para cerrar la conexión de manera segura
        public async Task<bool> Disconnect()
        {
            bool resultado = false;
            if (client != null)
            {
                try
                {
                    await client.DisconnectAsync().ConfigureAwait(false);
                    Debug.WriteLine("Desconectado satisfactoriamente.");
                    resultado = true;
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Error al desconectar del Broker: {e.Message}");
                    resultado = false;
                }
            }
            return resultado;
        }

        // metodo para conectarse al broker (hive mq)
        public async Task<bool> Connect()
        {
            if (client == null)
            {
                Debug.WriteLine("Error: El cliente MQTT no ha sido inicializado.");
                return false;
            }

            Debug.WriteLine($"Conectando a {options.Host} por el puerto {options.Port}...");

            try
            {
                var connectResult = await client.ConnectAsync().ConfigureAwait(false);
                if (connectResult.ReasonCode == ConnAckReasonCode.Success)
                {
                    Debug.WriteLine("Conectado satisfactoriamente.");
                    return true;
                }
                else
                {
                    Debug.WriteLine($"Error al conectar: {connectResult.ReasonCode}");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error al conectar al Broker: {e.Message}");
                return false;
            }
        }

        // Método para suscribirse al tema de comandos
        public async Task SubscribirComandos(string topic)
        {
            if (client == null) return;

            await client.SubscribeAsync(topic);
            TopicComandos = topic;
            Debug.WriteLine($"Suscrito al tema de comandos: '{topic}'");
        }

        public async Task SubscribirTelemetria(string topic)
        {
            if (client == null) return;

            await client.SubscribeAsync(topic);
            TopicTelemetria = topic;
            Debug.WriteLine($"Suscrito al tema de telemetría: '{topic}'");
        }

        // metodo para publicar un mensaje en comandos
        public async Task Publish(object payload)
        {
            if (client == null) return;

            var message = JsonSerializer.Serialize(payload);
            try
            {
                await client.PublishAsync(TopicComandos, message, QualityOfService.AtLeastOnceDelivery);
                Debug.WriteLine($"Mensaje publicado en el tema '{TopicComandos}': {message}");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error al publicar: {e.Message}");
            }
        }

        public async Task ObtenerDatos(CancellationToken token)
        {
            if (IsConnected)
            {
                while (!token.IsCancellationRequested)
                {
                    // enviar comando para recoger datos
                    await Publish("RECOLECTAR_DATOS");
                    await Task.Delay(5000, token); // se enviara este comando cada 5 segundos
                }
            }
            else
            {
                Debug.WriteLine("No se ha establecido ninguna conexion.");
            }
        }



    }
}

