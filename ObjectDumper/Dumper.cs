
namespace ObjectDumper
{
    /// <summary>
    /// Represents a wrapper-class for an object-dumper
    /// </summary>
    public static class Dumper
    {
        /// <summary>
        /// Dumps the given object as string
        /// </summary>
        /// <param name="value">Object</param>
        /// <returns>Dumped object string</returns>
        public static string Dump(this object value) => Dump(value, DumpMode.UTF8, 6);

        /// <summary>
        /// Dumps the given object as string
        /// </summary>
        /// <param name="value">Object</param>
        /// <param name="mode">The dump mode</param>
        /// <returns>Dumped object string</returns>
        public static string Dump(this object value, DumpMode mode) => Dump(value, mode, 6);

        /// <summary>
        /// Dumps the given object as string
        /// </summary>
        /// <param name="value">Object</param>
        /// <param name="depth">The maximum dump depth</param>
        /// <returns>Dumped object string</returns>
        public static string Dump(this object value, int depth) => Dump(value, DumpMode.UTF8, depth);

        /// <summary>
        /// Dumps the given object as string
        /// </summary>
        /// <param name="value">Object</param>
        /// <param name="mode">The dump mode</param>
        /// <param name="depth">The maximum dump depth</param>
        /// <returns>Dumped object string</returns>
        public static string Dump(this object value, DumpMode mode, int depth) => CoreLib.common.var_dump(value, mode == DumpMode.UTF8, depth);
    }

    /// <summary>
    /// Dump mode
    /// </summary>
    public enum DumpMode
        : byte
    {
        /// <summary>
        /// ASCII string dump
        /// </summary>
        ASCII,
        /// <summary>
        /// UTF-8 string dump
        /// </summary>
        UTF8,
    }
}
