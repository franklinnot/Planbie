using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Presentation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void AbrirVentanaPuertos(object sender, RoutedEventArgs e)
        {
            // Crear una nueva instancia de la ventana de Puertos
            EmergenteWindow puertosWindow = new EmergenteWindow();

            // Mostrar la ventana como modal (ventana emergente)
            puertosWindow.ShowDialog();
        }
    }
}