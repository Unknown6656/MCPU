﻿using System.ComponentModel.Composition.Hosting;
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
using System.Xml;
using System.IO;
using System;

using MCPU.Compiler;

using static System.Math;

using Settings = MCPU.IDE.Properties.Settings;

namespace MCPU.IDE
{
    /// <summary>
    /// Encapsulates the MCPU IDE application
    /// </summary>
    public partial class App
        : Application
    {
        internal static readonly (string, int, int, bool) DEFAULT_SETTINGS = (LanguageExtensions.DEFAULT_LANG,
                                                                              0x00100000, // 4 MB
                                                                              0x00020000, // 512 KB
                                                                              false);
        internal static Dictionary<string, string> def_compiler_table = typeof(MCPUCompiler).GetField("__defstrtable", BindingFlags.Static | BindingFlags.NonPublic)
                                                                                            .GetValue(null) as Dictionary<string, string>;
        internal static List<(string, string)> available_languages = new List<(string, string)>();
        internal ResourceDictionary previousdir = null;
        

        protected override void OnStartup(StartupEventArgs args)
        {
            DirectoryCatalog catalog = new DirectoryCatalog(AppDomain.CurrentDomain.BaseDirectory);
            CompositionContainer container = new CompositionContainer(catalog);

            container.ComposeParts(LanguageImportModule.Instance);

            AddResources();
            
            UpdateSaveSettings();
            ChangeLanguage(Settings.Default.Language);

            base.OnStartup(args);
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
                    else if ((m = Regex.Match(res, @"\/(?<code>.+)\.[xb]aml$", RegexOptions.Compiled | RegexOptions.IgnoreCase)).Success)
                        using (Stream ums = GetResourceStream(new Uri(res, UriKind.RelativeOrAbsolute)).Stream)
                        {
                            XmlDocument doc = new XmlDocument();

                            doc.Load(ums);
                            
                            available_languages.Add((m.Groups["code"].ToString(), (from XmlNode nd in doc.FirstChild.ChildNodes
                                                                                   let attr = nd.Attributes
                                                                                   where (from XmlAttribute a in attr
                                                                                          where a.Name == "x:Key"
                                                                                          where a.Value == "lang_name"
                                                                                          select a).FirstOrDefault() != null
                                                                                   select nd).FirstOrDefault()?.InnerText));
                        }

            available_languages = (from l in available_languages
                                   orderby l.Item1 ascending
                                   select l).ToList();
        }

        internal static void UpdateSaveSettings()
        {
            if (Settings.Default.UpgradeRequired)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpgradeRequired = false;
                Settings.Default.Save();
            }

            Settings.Default.Language = Settings.Default?.Language ?? DEFAULT_SETTINGS.Item1;
            Settings.Default.MemorySize = Max(512, Min(Processor.MAX_MEMSZ, Settings.Default?.MemorySize ?? DEFAULT_SETTINGS.Item2));
            Settings.Default.CallStackSize = Max(512, Min(Processor.MAX_STACKSZ, Settings.Default?.MemorySize ?? DEFAULT_SETTINGS.Item3));
            Settings.Default.OptimizeCode = Settings.Default?.OptimizeCode ?? DEFAULT_SETTINGS.Item4;
            Settings.Default.Save();
            Settings.Default.Reload();

            MCPUCompiler.OptimizationEnabled = Settings.Default.OptimizeCode;
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
