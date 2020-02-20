using System;
namespace env.linuxx86
{
    /// <summary>
    /// Abstracts the linux-x86 file system
    /// </summary>
    public static class Filesystem
    {
        /// <summary>
        /// Reads a single line from the file at the given path into the given line, reading at most line_size characters, or returns false.
        /// The file can have printf-style placeholders, in which case additional arguments must be passed.
        /// </summary>
        /// <param name="Line_size">the size of the line to read at most</param>
        /// <param name="Path_format">the path of the file to read, can contains fprintf style placeholders</param>
        /// <param name="Path_param">parameters for formatting the path, if it contains placeholders</param>
        /// <returns>the lines read in an array of bytes, null if error occured</returns>
        public static byte[] Tn_fs_readline(int Line_size, string Path_format, params string[] Path_param)
        {
            return null;
        }
    }
}
