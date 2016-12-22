using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows;
using System.Linq;
using System.Text;
using System.IO;
using System;

namespace MCPU.IDE
{
    using MCPU.Compiler;
    using MCPU;


    public partial class MainWindow
        : Window
    {
        public MainWindow() => InitializeComponent();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //tb_main.AddStyle();
        }
    }
}
