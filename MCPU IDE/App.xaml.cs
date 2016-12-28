using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Globalization;
using System.Configuration;
using System.Threading;
using System.Windows;
using System.Data;
using System.Linq;
using System;

namespace MCPU.IDE
{
    /// <summary>
    /// Encapsulates the MCPU IDE application
    /// </summary>
    public partial class App
        : Application
    {
        internal ResourceDictionary previousdir = null;
        
        protected override void OnStartup(StartupEventArgs args)
        {
            DirectoryCatalog catalog = new DirectoryCatalog(AppDomain.CurrentDomain.BaseDirectory);
            CompositionContainer container = new CompositionContainer(catalog);

            container.ComposeParts(LanguageImportModule.Instance);

            ChangeLanguage(LanguageExtensions.DEFAULT_LANG);

            base.OnStartup(args);
        }

        /// <summary>
        /// Changes the application's language to the given language (if found)
        /// </summary>
        /// <param name="code">New language code</param>
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
    
    /// <summary>
    /// Contains basic language management extension methods
    /// </summary>
    public static class LanguageExtensions
    {
        /// <summary>
        /// The application's and assembly's default language
        /// </summary>
        public const string DEFAULT_LANG = "en-GB";

        /// <summary>
        /// Returns the language-specific string associated with the given resource key
        /// </summary>
        /// <param name="key">Resource key</param>
        /// <returns>Language-specific string</returns>
        public static string GetStr(this string key) => Application.Current.Resources?[key] as string ?? "[string not found]";

        /// <summary>
        /// Returns the formatted language-specific string associated with the given resource key
        /// </summary>
        /// <param name="key">Resource key</param>
        /// <param name="args">Optional format string arguments</param>
        /// <returns>Formatted language-specific string</returns>
        public static string GetStr(this string key, params object[] args) => string.Format(GetStr(key), args);
    }

    /// <summary>
    /// 
    /// </summary>
    public sealed class LanguageImportModule
    {
        private static LanguageImportModule _instance;

        /// <summary>
        /// Static module instance
        /// </summary>
        public static LanguageImportModule Instance => _instance = _instance ?? new LanguageImportModule();
        /// <summary>
        /// 
        /// </summary>
        [ImportMany(typeof(ResourceDictionary))]
        public IEnumerable<Lazy<ResourceDictionary, IDictionary<string, object>>> ResourceDictionaryList { get; set; }
    }
}
