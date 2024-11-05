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

        public async Task<string> EnviarComando(string command)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
                throw new InvalidOperationException("Serial port is not open.");

            _serialPort.WriteLine(command);

            return await Task.Run(() => _serialPort.ReadLine());
        }

        public async Task EnviarComandoSR(string command)
        {
            if (_serialPort == null || !_serialPort.IsOpen)
                throw new InvalidOperationException("Serial port is not open.");

            _serialPort.WriteLine(command);
        }

        // metodo que devuelve el json con los datos quue el arduino devuelve ya serializados
        public async Task<JObject> ObtenerDatos()
        {
            string response = await EnviarComando("RECOLECTAR_DATOS");
            return JObject.Parse(response);
        }

        // metodo para activar o encender el buzzer
        public async Task EstadoBuzzer(bool estado)
        {
            if (estado)
            {
                await EnviarComandoSR("ENCENDER_BUZZER");
            }
            else {
                await EnviarComandoSR("APAGAR_BUZZER");
            }
        }

        // metodo para indicar en que estado debe estar el led
        public async Task EstadoLed(string estado)
        {
            if (estado == "CORRECTO")
            {
                await EnviarComandoSR("LED_VERDE");
            }
            else if (estado == "REGAR")
            {
                await EnviarComandoSR("LED_CELESTE");
            }
            else if (estado == "PELIGRO")
            {
                await EnviarComandoSR("LED_ROJO");
            }
        }

        // metodo usado cuando la temperatura sea muy alta y la humedad muy baja
        public async Task EstadoPeligro(bool peligro)
        {
            if (peligro) {
                await Regar(true);
                await EstadoBuzzer(true);
                await EstadoLed("PELIGRO");
            }
            else
            {
                await Regar(false);
                await EstadoBuzzer(false);
                await EstadoLed("CORRECTO");
            }
        }

        // metodo para activar o apagar la bomba de agua
        public async Task Regar(bool decision)
        {
            if (decision)
            {
                await EnviarComandoSR("REGAR");
                await EstadoBuzzer(true);
                await EstadoLed("CORRECTO");
            }
            else
            {
                await EnviarComandoSR("NO_REGAR");
                await EstadoBuzzer(false);
                await EstadoLed("CORRECTO");
            }
        }


    }
}
