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
using Microsoft.Win32;
using System.Windows;
using System.Linq;
using System.Text;
using System.IO;
using System;

using Settings = MCPU.IDE.Properties.Settings;

namespace MCPU.IDE
{
    public partial class SettingsWindow
        : Window
    {
        internal (string, int, int) settings;


        public SettingsWindow(MainWindow mainWindow)
        {
            InitializeComponent();

            Owner = mainWindow;
            settings = (Settings.Default.Language, Settings.Default.MemorySize, Settings.Default.CallStackSize);
        }
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            lst_lang.SelectionMode = SelectionMode.Single;
            lst_lang.Items.Clear();

            foreach (string lang in App.available_languages)
                lst_lang.Items.Add(new LanguageInfo
                {
                    Code = lang,
                    VisibleName = "<<< TODO >>>",
                    Image = new BitmapImage(new Uri($"Resources/{lang}.png", UriKind.RelativeOrAbsolute)),
                });

            lst_lang.SelectedItem = (from LanguageInfo i in lst_lang.Items
                                     where i.Code.Equals("lang_code".GetStr(), StringComparison.InvariantCultureIgnoreCase)
                                     select i).FirstOrDefault();
            lst_lang.Focus();
        }
        
        private void lst_lang_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lst_lang.SelectedIndex != -1)
            {
                settings.Item1 = (lst_lang.SelectedItem as LanguageInfo)?.Code as string ?? LanguageExtensions.DEFAULT_LANG;

                (Application.Current as App).ChangeLanguage(settings.Item1);
            }
        }
        
        private void Button_cancel_Click(object sender, RoutedEventArgs e) => Close();

        private void Button_reset_Click(object sender, RoutedEventArgs e) => settings = App.DEFAULT_SETTINGS;

        private void Button_save_Click(object sender, RoutedEventArgs e)
        {
            (Settings.Default.Language,
             Settings.Default.MemorySize,
             Settings.Default.CallStackSize) = settings;
            
            App.UpdateSaveSettings();

            Close();
        }
    }

    public class LanguageInfo
    {
        public string Code { set; get; }
        public string VisibleName { set; get; }
        public ImageSource Image { set; get; }
    }
}
