using MQTT;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Presentation.Logica
{
    public class ArduinoControl
    {
        public SerialPort _serialPort;
        public string Port { get; private set; }
        public bool IsConnected => _serialPort?.IsOpen ?? false;
        public event Action<string>? OnDataReceived;

        // singleton -- instancia única
        private static readonly ArduinoControl _instancia = new ArduinoControl();
        public static ArduinoControl Instancia => _instancia;


        private ArduinoControl() {
            
        }

        public void Inicializar(string portName)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                Debug.WriteLine("El puerto serie ya está inicializado y abierto.");
                return;
            }

            _serialPort = new SerialPort(portName, 9600)
            {
                ReadTimeout = 1000, // tiempo de lectura
                WriteTimeout = 1000 // escritura
            };

            _serialPort.DataReceived += (sender, args) =>
            {
                try
                {
                    string data = _serialPort.ReadLine();
                    OnDataReceived?.Invoke(data); // Notifica a los suscriptores
                }
                catch (TimeoutException)
                {
                    Debug.WriteLine("Se alcanzó el tiempo de espera al leer del puerto.");
                }
            };

            Port = portName;
            Debug.WriteLine("Se creó una instancia de ArduinoControl");
            Debug.WriteLine($"Puerto serie configurado: {portName} a 9600 baudios.");
        }


        // metodo para conectar y abrir el puerto serie
        public void Connect()
        {
            if (_serialPort == null)
            {
                Debug.WriteLine("Error: El puerto serie no ha sido inicializado.");
                throw new InvalidOperationException("Debe inicializar el puerto antes de conectarse.");
            }

            if (!_serialPort.IsOpen)
            {
                _serialPort.Open();
                Debug.WriteLine("Puerto serie abierto satisfactoriamente.");
            }
        }

        // Método para cerrar la conexión
        public void Disconnect()
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();

                Port = string.Empty;
                Debug.WriteLine("Puerto serie cerrado.");
            }
        }

        // metodo para enviar un comando
        public async Task EnviarComando(string command)
        {
            if (!IsConnected)
                throw new InvalidOperationException("El puerto serie no está abierto.");
              
            await Task.Run(() => _serialPort.WriteLine(command));
            Debug.WriteLine($"Mensaje publicado en {Port}: {command}");
        }

        // metodo que devuelve el json con los datos quue el arduino devuelve ya serializados
        public async Task ObtenerDatos(CancellationToken token)
        {
            if (IsConnected)
            {
                while (!token.IsCancellationRequested)
                {
                    // enviar comando para recoger datos
                    await EnviarComando("RECOLECTAR_DATOS");
                    await Task.Delay(5000, token); // se enviara este comando cada 5 segundos
                }
            }
            else
            {
                Debug.WriteLine("El puerto no está abierto.");
            }
        }

        // metodo para activar o encender el buzzer
        public async Task EstadoBuzzer(bool estado)
        {
            string command = estado ? "BUZZER_ON" : "BUZZER_OFF";
            await EnviarComando(command);
        }

        // metodo para indicar en que estado debe estar el led
        public async Task EstadoLed(string estado)
        {
            await EnviarComando(estado);
        }

        // metodo usado cuando la temperatura sea muy alta y la humedad muy baja
        public async Task EstadoPeligro(bool peligro)
        {
            await Regar(peligro);
        }

        // metodo para activar o apagar la bomba de agua
        public async Task Regar(bool decision)
        {
            string command = decision ? "REGAR_ON" : "REGAR_OFF";
            await EnviarComando(command);
            await EstadoLed(decision ? "RGB_BLUE" : "RGB_VERDE");
        }


    }
}
