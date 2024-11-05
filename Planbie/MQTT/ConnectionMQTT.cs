using HiveMQtt.Client;
using HiveMQtt.Client.Options;
using HiveMQtt.MQTT5.ReasonCodes;
using HiveMQtt.MQTT5.Types;
using System.Diagnostics;
using System.Text.Json;

namespace MQTT
{
    public class ConectionMQTT
    {
        private readonly HiveMQClient client;
        private readonly HiveMQClientOptions options;

        // evento para cuando se recibe un mensaje, se incluye tambien el topic de origen
        public event Action<string, string>? OnMessageReceived; // (topic, message)

        public ConectionMQTT(string? host = null, int? port = null, string? username = null, string? password = null)
        {
            options = new HiveMQClientOptions
            {
                Host = host == null ? "aff52f09c9304bdeb05152362309f8c6.s1.eu.hivemq.cloud" : host,
                Port = port == null ? 8883 : (int)port,
                UseTLS = true,
                UserName = username == null ? "ana" : username,
                Password = password == null ? "ana" : password
            };

            client = new HiveMQClient(options);

            client.OnMessageReceived += (sender, args) =>
            {
                string mensajeRecibido = args.PublishMessage.PayloadAsString;
                string? topic = args.PublishMessage.Topic;
                topic = topic ?? "Topic desconocido";
                OnMessageReceived?.Invoke(topic, mensajeRecibido);
            };
        }


        // metodo para conectarse al broker (hive mq)
        public async Task<bool> Connect()
        {
            Debug.Write($"Conectando a {options.Host} por el puerto {options.Port}...");

            try
            {
                var connectResult = await client.ConnectAsync().ConfigureAwait(false);
                if (connectResult.ReasonCode == ConnAckReasonCode.Success)
                {
                    Debug.Write("Conectado satisfactoriamente.");
                    return true;
                }
                else
                {
                    Debug.Write($"Error al conectar: {connectResult.ReasonCode}");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.Write($"Error al conectar al Broker: {e.Message}");
                return false;
            }
        }

        // metodo para suscribirse a un tema / topic
        public async Task Subscribe(string topic)
        {
            await client.SubscribeAsync(topic);
            Debug.Write($"Suscrito al tema '{topic}'");
        }

        // metodo para publicar un mensaje en un tema / topic específico
        public async Task Publish(string topic, object payload)
        {
            var message = JsonSerializer.Serialize(payload);
            try
            {
                var result = await client.PublishAsync(topic, message, QualityOfService.AtLeastOnceDelivery);
                Debug.Write($"Mensaje publicado en el tema '{topic}': {message}");
            }
            catch (Exception e)
            {
                Debug.Write($"Error al publicar: {e.Message}");
            }
        }
    }
}

