﻿using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Linq;
using System;

using Settings = MCPU.IDE.Properties.Settings;

namespace MCPU.IDE
{
    public partial class SettingsWindow
        : Window
    {
        internal (string, int, int, bool) settings;


        public SettingsWindow(MainWindow mainWindow)
        {
            InitializeComponent();

            Owner = mainWindow;
            settings = (Settings.Default.Language, Settings.Default.MemorySize, Settings.Default.CallStackSize, Settings.Default.OptimizeCode);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            mtb_memsz.Text = settings.Item2.ToString();
            mtb_memsz.TextChanged += MaskedTextBox_TextChanged;

            mtb_stacksz.Text = settings.Item3.ToString();
            mtb_stacksz.TextChanged += MaskedTextBox_TextChanged;

            lst_lang.SelectionMode = SelectionMode.Single;
            lst_lang.Items.Clear();

            cb_optcode.IsChecked = settings.Item4;
            cb_optcode.Checked += (s, a) => settings.Item4 = true;
            cb_optcode.Unchecked += (s, a) => settings.Item4 = false;

            foreach ((string code, string lang) in App.available_languages)
                lst_lang.Items.Add(new LanguageInfo
                {
                    Code = code,
                    VisibleName = lang,
                    Image = new BitmapImage(new Uri($"Resources/{code}.png", UriKind.RelativeOrAbsolute)),
                });

            lst_lang.SelectedItem = (from LanguageInfo i in lst_lang.Items
                                     where i.Code.Equals("lang_code".GetStr(), StringComparison.InvariantCultureIgnoreCase)
                                     select i).FirstOrDefault();
            lst_lang.Focus();
        }

        private void MaskedTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox mtb)
            {
                string nv = new string((from c in mtb.Text
                                        where char.IsDigit(c)
                                        select c).ToArray());
                // int len = mtb == mtb_memsz ? 10 : 7;
                // mtb.Text = nv.Length > len ? nv.Remove(len) : nv;

                if (nv.Length == 0)
                    nv = "0";

                mtb.Text = nv;

                long val = long.Parse(nv);

                if (val > int.MaxValue)
                    val = int.MaxValue;

                if (mtb == mtb_memsz)
                    settings.Item2 = (int)val;
                else
                    settings.Item3 = (int)val;
            }
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
             Settings.Default.CallStackSize,
             Settings.Default.OptimizeCode) = settings;

            App.UpdateSaveSettings();

            if (Owner is MainWindow mwin)
                mwin.InitProcessor();

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
