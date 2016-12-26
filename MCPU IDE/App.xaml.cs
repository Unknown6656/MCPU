using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace MCPU.IDE
{
    public partial class App
        : Application
    {
        internal ResourceDictionary previousdir = null;
        internal const string DEFAULT_LANG = "de-DE";

        
        protected override void OnStartup(StartupEventArgs args)
        {
            DirectoryCatalog catalog = new DirectoryCatalog(AppDomain.CurrentDomain.BaseDirectory);
            CompositionContainer container = new CompositionContainer(catalog);

            container.ComposeParts(LanguageImportModule.Instance);

            ChangeLanguage(DEFAULT_LANG);

            base.OnStartup(args);
        }

        public void ChangeLanguage(string code)
        {
            var resdir = (from d in LanguageImportModule.Instance.ResourceDictionaryList
                          where d.Metadata.ContainsKey("Culture") &&
                                d.Metadata["Culture"].ToString().ToLower() == code.ToLower()
                          select d).FirstOrDefault();

            if (resdir?.Value == null)
                resdir = new Lazy<ResourceDictionary, IDictionary<string, object>>
                    (
                        metadata: new Dictionary<string, object> { { "Culture", code } },
                        valueFactory: () => new ResourceDictionary { Source = new Uri($@"/mcpu.ide;component/Languages/{code}.xaml", UriKind.RelativeOrAbsolute) }
                    );

            if (previousdir != null)
                Current.Resources.MergedDictionaries.Remove(previousdir);

            Current.Resources.MergedDictionaries.Add(previousdir = resdir.Value);

            CultureInfo cultureInfo = new CultureInfo(code);

            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;

            foreach (Window hwnd in Current.Windows)
                if (hwnd != null)
                    hwnd.Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);
        }
    }
    
    public static class Language
    {
        public static string Get(this string key) => Application.Current.Resources[key] as string;

        public static string Get(this string key, params object[] args) => string.Format(Get(key), args);
    }

    public sealed class LanguageImportModule
    {
        private static LanguageImportModule _instance;

        public static LanguageImportModule Instance =>
            _instance = _instance ?? new LanguageImportModule();

        
        [ImportMany(typeof(ResourceDictionary))]
        public IEnumerable<Lazy<ResourceDictionary, IDictionary<string, object>>> ResourceDictionaryList { get; set; }
    }
}
