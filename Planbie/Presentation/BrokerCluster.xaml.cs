using MQTT;
using System.Diagnostics;
using System.Windows;

namespace Presentation
{
    public partial class BrokerCluster : Window
    {
        public static bool conexionCerrada = false;
        public BrokerCluster()
        {
            InitializeComponent();

            panel_conexionBroker.Visibility = Visibility.Visible;

            if (ConnectionMQTT.Instancia.IsConnected)
            {
                BloquearTextBoxs(true);
            }
        }

        private async void btnConectarMqtt_Click(object sender, RoutedEventArgs e)
        {
            string url = txtClusterUrl.Text.Trim();
            int.TryParse(txtPort.Text, out int castPort);
            int port = castPort != 0 ? castPort : 0;
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text.Trim();
            string topicTelemetria = txt_topicTelemetria.Text.Trim();
            string topicComandos = txt_topicComandos.Text.Trim();

            if (string.IsNullOrEmpty(url) || port == 0 || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(topicTelemetria) || string.IsNullOrEmpty(topicComandos)) {
                MessageBox.Show("Debes ingresar correctamente todos los campos."); 
                return;
            }

            btnConectarMqtt.IsEnabled = false;
            try
            {
                ConnectionMQTT.Instancia.Inicializar(host: url, port: port, username: username, password: password);
                bool conexionExitosa = await ConnectionMQTT.Instancia.Connect();
                bool suscripcionComandos = await ConnectionMQTT.Instancia.SubscribirComandos(topicComandos);
                bool suscripcionTelemetria = await ConnectionMQTT.Instancia.SubscribirTelemetria(topicTelemetria);
                if (conexionExitosa && suscripcionComandos && suscripcionTelemetria)
                {
                    BloquearTextBoxs(true);
                    conexionCerrada = false;
                    MessageBox.Show("Conectado exitosamente al broker MQTT.");
                }
                else if (conexionExitosa && !suscripcionComandos && suscripcionTelemetria) 
                {
                    MessageBox.Show($"Error al suscribirse al tópico de comandos: {topicComandos}");
                }
                else if (conexionExitosa && suscripcionComandos && !suscripcionTelemetria)
                {
                    MessageBox.Show($"Error al suscribirse al tópico de telemetria: {topicTelemetria}");
                }
                else
                {
                    MessageBox.Show("Error al conectar al broker MQTT.");
                }

            }
            catch (Exception ex) {
                MessageBox.Show("Hubo un error al intentar conectarse al Broker.");
                Debug.WriteLine($"Hubo un error al intentar conectarse al Broker: {ex.Message}");
            }

            btnConectarMqtt.IsEnabled = true;
        }

        private async void btnDesconectarMqtt_Click(object sender, RoutedEventArgs e)
        {
            btnDesconectarMqtt.IsEnabled = false;
            await ConnectionMQTT.Instancia.Disconnect();
            BloquearTextBoxs(false);
            conexionCerrada = true;
        }

        private void BloquearTextBoxs(bool bloquar)
        {
            if (ConnectionMQTT.Instancia.IsConnected) {
                txtClusterUrl.Text = ConnectionMQTT.Instancia.Host;
                txtPort.Text = ConnectionMQTT.Instancia.Port.ToString();
                txtUsername.Text = ConnectionMQTT.Instancia.Username;
                txtPassword.Text = ConnectionMQTT.Instancia.Password;
                txt_topicComandos.Text = ConnectionMQTT.Instancia.TopicComandos;
                txt_topicTelemetria.Text = ConnectionMQTT.Instancia.TopicTelemetria;
            }

            bloquar = !bloquar;
            txtClusterUrl.IsEnabled = bloquar;
            txtPort.IsEnabled = bloquar;
            txtUsername.IsEnabled = bloquar;
            txtPassword.IsEnabled = bloquar;
            txt_topicComandos.IsEnabled = bloquar;
            txt_topicTelemetria.IsEnabled = bloquar;

            bloquar = !bloquar;
            // si se esta bloqueando, entonces debe estar conectado
            if (bloquar) {
                btnConectarMqtt.Visibility = Visibility.Hidden;
                btnDesconectarMqtt.Visibility = Visibility.Visible;
            }
            else
            {
                btnDesconectarMqtt.Visibility = Visibility.Hidden;
                btnConectarMqtt.Visibility = Visibility.Visible;
            }


        }

        private void btn_cerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}
