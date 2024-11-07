using Presentation.Logica;
using System.Diagnostics;
using System.IO.Ports;
using System.Windows;
using System.Windows.Media;

namespace Presentation
{
    public partial class EmergenteWindow : Window
    {
        private string[] puertosAnteriores = { "Carga de puertos" };
        private System.Timers.Timer? puertoTimer;
        public static bool conexionCerrada = false;
        public EmergenteWindow()
        {
            InitializeComponent();
            ActualizarPuertos();

            // si el puertoSerial ya esta conectados, modificamos el diseño
            if (ArduinoControl.Instancia.IsConnected)
            {
                UIPuertoAbierto();
            }

        }

        #region Cargar los puertos disponibles en el ComboBox

        private void ActualizarPuertos()
        {
            puertoTimer = new System.Timers.Timer(300);
            puertoTimer.AutoReset = true; // Esta línea asegura que el temporizador se reinicie automáticamente
            puertoTimer.Elapsed += async (sender, e) => await CargarPuertosDisponibles();
            puertoTimer.Start();
        }

        // metodo para cargar los puertos
        private async Task CargarPuertosDisponibles()
        {
            string[] puertosActuales = SerialPort.GetPortNames();

            // compara si hay un cambio en la lista de puertos
            if (!puertosActuales.SequenceEqual(puertosAnteriores))
            {
                puertosAnteriores = puertosActuales;

                // actualizar el ComboBox en el hilo de la interfaz
                await Dispatcher.InvokeAsync(() =>
                {
                    if (puertosActuales.Length > 0)
                    {
                        Debug.WriteLine("Se cargaron nuevos puertos");
                        cmbPuertos.ItemsSource = puertosActuales;
                        btnAbrirPuerto.IsEnabled = true;
                    }
                    else
                    {
                        Debug.WriteLine("No se han encontrado puertos disponibles");
                        cmbPuertos.ItemsSource = new string[] { "No hay puertos disponibles" };
                        btnAbrirPuerto.IsEnabled = false;
                    }

                    // si el puerto ya esta abierto, seleccionarlo en el combo
                    if (ArduinoControl.Instancia.IsConnected)
                    {
                        cmbPuertos.SelectedItem = ArduinoControl.Instancia.Port;
                    }
                    else
                    {
                        cmbPuertos.SelectedIndex = 0;
                    }
                });
            }

        }

        #endregion

        #region Abrir/cerrar el puerto serial al hacer clic en el botón

        private void AbrirPuerto_Click(object sender, RoutedEventArgs e)
        {
            // si el puerto ya esta abierto, cerrarlo
            if (ArduinoControl.Instancia.IsConnected)
            {
                CerrarPuertoSerial();
            }
            // si no ha sido abierto ningun puerto, abrimos uno
            else
            {
                AbrirPuertoSerial();
            }
        }

        private void AbrirPuertoSerial()
        {
            string? puertoSeleccionado = cmbPuertos.SelectedItem.ToString();

            if (!string.IsNullOrEmpty(puertoSeleccionado))
            {
                try
                {
                    // inicializar el puerto serial y abrirlo
                    ArduinoControl.Instancia.Inicializar(puertoSeleccionado);
                    ArduinoControl.Instancia.Connect();

                    Debug.WriteLine($"Puerto {puertoSeleccionado} abierto con éxito.");

                    // Deshabilitar el ComboBox y cambiar el texto del botón
                    UIPuertoAbierto();
                    conexionCerrada = false; // indicamos que la conexion fue abierta para que se siga recolectando datos
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

        private void CerrarPuertoSerial()
        {
            try
            {
                // lo cerramos
                MainWindow.cts?.Cancel();
                ArduinoControl.Instancia.Disconnect();
                Debug.WriteLine("Puerto cerrado con éxito.");

                // restaurar el estado de la interfaz al cerrar el puerto
                cmbPuertos.IsEnabled = true;
                btn_portText.Text = "Abrir puerto";
                btnAbrirPuerto.Background = (Brush)new BrushConverter().ConvertFrom("#FF660CA7");
                conexionCerrada = true; // indicamos que la conexion fue cerrada para evitar que se siga recolectando datos

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cerrar el puerto: {ex.Message}");
            }
        }

        #endregion

        // metodo para inhabilitar el combobox y modificar el color y texto del boton si el puerto esta abierto
        private void UIPuertoAbierto()
        {
            cmbPuertos.SelectedItem = ArduinoControl.Instancia.Port; 
            cmbPuertos.IsEnabled = false;     
            btn_portText.Text = "Cerrar puerto";
            btnAbrirPuerto.Background = (Brush)new BrushConverter().ConvertFrom("#FFD62935");
        }

        // Cerrar  la ventana
        private void btn_cerrar_Click(object sender, RoutedEventArgs e)
        {
            puertoTimer?.Stop();
            puertoTimer?.Dispose();
            base.OnClosed(e);

            this.Close();
        }

        #region enventos enter & leave en el boton de abrir puerto
        private void btnAbrirPuerto_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            hoverBotonPort("enter");
        }

        private void btnAbrirPuerto_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            hoverBotonPort("leave");
        }

        private void hoverBotonPort(string accion)
        {
            if (btn_portText.Text == "Cerrar puerto") {
                if (accion == "enter")
                {
                    btnAbrirPuerto.Background = (Brush)new BrushConverter().ConvertFrom("#FFe63946");
                } 
                else if (accion == "leave") {
                    btnAbrirPuerto.Background = (Brush)new BrushConverter().ConvertFrom("#FFD62935");
                }
            }
            else if (btn_portText.Text == "Abrir puerto")
            {
                if (accion == "enter")
                {
                    btnAbrirPuerto.Background = (Brush)new BrushConverter().ConvertFrom("#FF740DBF");
                }
                else if (accion == "leave")
                {
                    btnAbrirPuerto.Background = (Brush)new BrushConverter().ConvertFrom("#FF660CA7");
                }
            }
        }

        #endregion

    }
}
