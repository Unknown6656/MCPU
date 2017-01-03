using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Globalization;
using System.Collections;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Windows;
using System.Data;
using System.Linq;
using System.IO;
using System;

using MCPU.Compiler;

using Settings = MCPU.IDE.Properties.Settings;

namespace MCPU.IDE
{
    /// <summary>
    /// Encapsulates the MCPU IDE application
    /// </summary>
    public partial class App
        : Application
    {
        internal static readonly (string, int) DEFAULT_SETTINGS = (LanguageExtensions.DEFAULT_LANG, 0x100000 /* 4MB */);

        internal static Dictionary<string, string> def_compiler_table = typeof(MCPUCompiler).GetField("__defstrtable", BindingFlags.Static | BindingFlags.NonPublic)
                                                                                            .GetValue(null) as Dictionary<string, string>;
        internal static List<string> available_languages = new List<string>();
        internal ResourceDictionary previousdir = null;
        

        protected override void OnStartup(StartupEventArgs args)
        {
            DirectoryCatalog catalog = new DirectoryCatalog(AppDomain.CurrentDomain.BaseDirectory);
            CompositionContainer container = new CompositionContainer(catalog);

            container.ComposeParts(LanguageImportModule.Instance);

            AddResources();
            
            Settings.Default.Language = Settings.Default?.Language ?? DEFAULT_SETTINGS.Item1;
            Settings.Default.MemorySize = Settings.Default?.MemorySize ?? DEFAULT_SETTINGS.Item2;

            UpdateSaveSettings();
            ChangeLanguage(Settings.Default.Language);

            base.OnStartup(args);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            IDE.Properties.Settings.Default.Language = "lang_code".GetStr();

            base.OnExit(e);
        }
        
        internal static void AddResources()
        {
            Assembly asm = typeof(App).Assembly;
            string resbase = asm.GetName().Name + ".g.resources";
            const int img_size = 20;
            Match m;

            using (Stream stream = asm.GetManifestResourceStream(resbase))
            using (ResourceReader reader = new ResourceReader(stream))
                foreach (string res in reader.Cast<DictionaryEntry>().Select(entry => (string)entry.Key))
                    if ((m = Regex.Match(res, @"\/(?<name>.+)\.(?<ext>(png|bmp|gif|je?pg))", RegexOptions.Compiled | RegexOptions.IgnoreCase)).Success)
                        Current.Resources.Add(m.Groups["name"].ToString(), new Image
                        {
                            Width = img_size,
                            Height = img_size,
                            Source = new BitmapImage(new Uri($"Resources/{m.Groups["name"]}.{m.Groups["ext"]}", UriKind.RelativeOrAbsolute)),
                        });
                    else if ((m = Regex.Match(res, @"\/(?<name>.+)\.[xb]aml$", RegexOptions.Compiled | RegexOptions.IgnoreCase)).Success)
                        available_languages.Add(m.Groups["name"].ToString());
        }

        internal static void UpdateSaveSettings()
        {
            Settings.Default.Save();
            Settings.Default.Reload();
        }

        /// <summary>
        /// Changes the application's language to the given language (if found)
        /// </summary>
        /// <param name="code">New language code</param>
        public void ChangeLanguage(string code = null)
        {
            code = code ?? LanguageExtensions.DEFAULT_LANG;

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
                {
                    hwnd.Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);

                    if (hwnd is ILanguageSensitiveWindow ilswnd)
                        ilswnd.OnLanguageChanged(code);
                }

            ResourceDictionary compiler_dic = Current.Resources["global_compiler_msg"] as ResourceDictionary;
            Dictionary<string, string> compiler_table = (from object key in compiler_dic?.Keys ?? new object[0]
                                                         select new {
                                                             Key = key as string,
                                                             Value = compiler_dic?[key] as string
                                                         }).ToDictionary(_ => _.Key, _ => _.Value);

            if (def_compiler_table.Keys.All(_ => compiler_table.ContainsKey(_)))
                MCPUCompiler.SetLanguage(compiler_table);
            else
                MCPUCompiler.ResetLanguage();
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
        public const string DEFAULT_LANG = "de-DE";

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
    /// Represents the language import module
    /// </summary>
    public sealed class LanguageImportModule
    {
        private static LanguageImportModule _instance;

        /// <summary>
        /// Static module instance
        /// </summary>
        public static LanguageImportModule Instance => _instance = _instance ?? new LanguageImportModule();
        /// <summary>
        /// The resource dictionary list
        /// </summary>
        [ImportMany(typeof(ResourceDictionary))]
        public IEnumerable<Lazy<ResourceDictionary, IDictionary<string, object>>> ResourceDictionaryList { get; set; }
    }

    /// <summary>
    /// Represents a interface, which can notify the targeted window (or any other class) of language changements
    /// </summary>
    public interface ILanguageSensitiveWindow
    {
        /// <summary>
        /// Event handler, which will be called if the language changes to the given code
        /// </summary>
        /// <param name="code">New language code</param>
        void OnLanguageChanged(string code);
    }
}
