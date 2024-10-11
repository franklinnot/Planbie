using System;
using System.IO.Ports;
using System.Windows;

namespace Presentation
{
    public partial class EmergenteWindow : Window
    {
        private SerialPort puertoSerial;

        public EmergenteWindow()
        {
            InitializeComponent();
            CargarPuertosDisponibles();  // Cargar puertos al iniciar
        }

        // Método para cargar los puertos disponibles en el ComboBox
        private void CargarPuertosDisponibles()
        {
            string[] puertos = SerialPort.GetPortNames(); // Obtener los nombres de los puertos disponibles

            if (puertos.Length > 0)  // Si hay puertos disponibles
            {
                cmbPuertos.ItemsSource = puertos;   // Cargar los puertos en el ComboBox
                cmbPuertos.SelectedIndex = 0;       // Seleccionar el primer puerto por defecto
                btnAbrirPuerto.IsEnabled = true;    // Habilitar el botón para abrir/cerrar
            }
            else  // Si no hay puertos disponibles
            {
                cmbPuertos.ItemsSource = new string[] { "No hay puertos disponibles" };  // Mostrar mensaje en el ComboBox
                cmbPuertos.SelectedIndex = 0;
                 
                btnAbrirPuerto.IsEnabled = false;    // Deshabilitar el botón
            }
        }

        // Evento para recargar puertos disponibles al hacer clic en el ComboBox
        private void cmbPuertos_DropDownOpened(object sender, EventArgs e)
        {
            CargarPuertosDisponibles();  // Recargar los puertos cuando se abre el ComboBox
        }

        // Método para abrir/cerrar el puerto serial al hacer clic en el botón
        private void AbrirPuerto_Click(object sender, RoutedEventArgs e)
        {
            // Si el puerto ya está abierto, cerrarlo
            if (puertoSerial != null && puertoSerial.IsOpen)
            {
                try
                {
                    puertoSerial.Close();
                    MessageBox.Show("Puerto cerrado con éxito.");

                    // Rehabilitar el ComboBox y cambiar el texto del botón
                    cmbPuertos.IsEnabled = true;
                    btnAbrirPuerto.Content = "Abrir Conexión";
                    CargarPuertosDisponibles();  // Recargar puertos disponibles
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cerrar el puerto: {ex.Message}");
                }
            }
            else
            {
                string puertoSeleccionado = cmbPuertos.SelectedItem as string;

                if (!string.IsNullOrEmpty(puertoSeleccionado) && puertoSeleccionado != "No hay puertos disponibles")
                {
                    try
                    {
                        // Inicializar el puerto serial y abrirlo
                        puertoSerial = new SerialPort(puertoSeleccionado);
                        puertoSerial.Open();

                        MessageBox.Show($"Puerto {puertoSeleccionado} abierto con éxito.");
                        // Deshabilitar el ComboBox y cambiar el texto del botón
                        cmbPuertos.IsEnabled = false;
                        btnAbrirPuerto.Content = "Cerrar Conexión";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al abrir el puerto: {ex.Message}");
                    }
                }
                else
                {
                    MessageBox.Show("Selecciona un puerto válido.");
                }
            }
        }

        // Método para cerrar la ventana
        private void btn_cerrar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
