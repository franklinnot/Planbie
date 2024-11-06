using MQTT;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Presentation
{
    public partial class BrokerCluster : Window
    {
        private ConnectionMQTT mqttClient;
        public ConnectionMQTT MqttClient => mqttClient;

        public BrokerCluster()
        {
            InitializeComponent();
        }

        private void btn_cerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void btnConectarMqtt_Click(object sender, RoutedEventArgs e)
        {
            string url = txtClusterUrl.Text.Trim();
            int.TryParse(txtPort.Text, out int castPort);
            int port = castPort != 0 ? castPort : 0;
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(url) || port == 0 || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password)) {
                MessageBox.Show("Debes ingresar correctamente todos los campos."); 
                return;
            }

            try
            {
                mqttClient = new ConnectionMQTT(host: url, port: port, username: username, password: password);
                bool conexionExitosa = await mqttClient.Connect();

                if (conexionExitosa)
                {
                    MessageBox.Show("Conectado exitosamente al broker MQTT.");

                    panel_conexionBroker.Visibility = Visibility.Hidden;
                    panel_subscripcionTopicos.Visibility = Visibility.Visible;
                }
                else
                {
                    MessageBox.Show("Error al conectar al broker MQTT.");
                }

                // si todo sale bien
            }
            catch (Exception ex) {
                MessageBox.Show("Hubo un error al intentar conectarse al Broker.");
                Debug.WriteLine($"Hubo un error al intentar conectarse al Broker: {ex.Message}");
            }
        }

        private void btnSuscribirseTopicos_Click(object sender, RoutedEventArgs e)
        {
            string topic_telemetria = txt_topicTelemetria.Text.Trim();
            string topic_comandos = txt_topicComandos.Text.Trim();
        }
    }
}
