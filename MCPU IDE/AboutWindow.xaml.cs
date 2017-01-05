using System.Windows.Navigation;
using System.Windows.Input;
using System.Reflection;
using System.Windows;
using System;

namespace MCPU.IDE
{
    public partial class AboutWindow
        : Window
    {
        internal new MainWindow Parent { get; }


        public AboutWindow(MainWindow par)
        {
            Owner = par;
            Parent = par;
            InitializeComponent();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e) => Parent.mih_github(sender, null);

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            tb_version.Text = "aw_version_str".GetStr(AssemblyName.GetAssemblyName(Assembly.GetExecutingAssembly().Location).Version);
            curr_year.Text = DateTime.Now.Year.ToString();
            hl_github.NavigateUri = new Uri("github_base_url".GetStr());
            hl_github_issues.NavigateUri = new Uri("github_base_url".GetStr() + "/issues");
        }

        private void Button_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}
