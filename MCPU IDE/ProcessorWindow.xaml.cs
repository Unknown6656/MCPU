using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Shapes;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Data;
using System.Diagnostics;
using System.Windows;
using System.Linq;
using System.Text;
using System.IO;
using System;

namespace MCPU.IDE
{
    public partial class ProcessorWindow
        : Window
    {
        internal MainWindow mwin;


        public ProcessorWindow(MainWindow mainWindow)
        {
            InitializeComponent();

            mwin = mainWindow;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        internal void Proc_InstructionExecuted(Processor p, Instruction args)
        {

        }

        internal void Proc_ProcessorReset(Processor p)
        {

        }

        internal void Proc_OnError(Processor p, Exception args)
        {

        }
    }
}
