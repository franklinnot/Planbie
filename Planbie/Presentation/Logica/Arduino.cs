using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Presentation.Logica
{
    public class Arduino
    {
        private SerialPort _serialPort;

        public Arduino(SerialPort serialPort)
        {
            _serialPort = serialPort;
        }

        public async Task<string> SendCommand(string command)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
                throw new InvalidOperationException("Serial port is not open.");

            _serialPort.WriteLine(command);

            return await Task.Run(() => _serialPort.ReadLine());
        }


        public async Task SendCommandSR(string command)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
                throw new InvalidOperationException("Serial port is not open.");

            _serialPort.WriteLine(command);
        }

        public async Task<JObject> CollectData()
        {
            string response = await SendCommand("RECOLECTAR_DATOS");
            return JObject.Parse(response);
        }

        public async Task TurnOffLED()
        {
            await SendCommand("APAGAR_LED");
        }

        public async Task TurnOffBuzzer()
        {
            await SendCommandSR("APAGAR_BUZZER");

        }
        public async Task Alert_Temp()
        {
            await SendCommandSR("ALERTA_TEMPERATURA");
        }
        public async Task Alert_Buzzer()
        {
            await SendCommandSR("ALERTA_BUZZER");
        }

        public async Task Good_State()
        {
            await SendCommandSR("ESTADO_CORRECTO");
        }

        public async Task Regando(int value)
        {
            if (value == 1)
            {
                await SendCommandSR("REGANDO");
            }
            else if (value == 0)
            {
                await SendCommandSR("NO_REGAR");
            }
        }

        public async Task StartDataCollection(Action<JObject> onDataReceived)
        {
            while (_serialPort.IsOpen)
            {
                try
                {
                    JObject data = await CollectData();
                    onDataReceived(data);
                    await Task.Delay(1000); // Collect data every second
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error collecting data: {ex.Message}");
                    // Considerar agregar un mecanismo para notificar errores a la UI
                }
            }
        }
    }
}
