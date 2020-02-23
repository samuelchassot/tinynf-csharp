using Utilities;
using System.IO;

namespace Env.linuxx86
{
    /// <summary>
    /// Abstracts the linux-x86 file system
    /// </summary>
    public static class Filesystem
    {
        private static Logger log = new Logger(Constants.logLevel);
        /// <summary>
        /// Reads a single line from the file at the given path into the given line, reading at most line_size characters, or returns false.
        /// The file can have c#-style placeholders ("this is an example of placeholders {0} with another one here {1}"), in which case additional arguments must be passed.
        /// </summary>
        /// <param name="line_size">the size of the line to read at most</param>
        /// <param name="path_format">the path of the file to read, can contains fprintf style placeholders</param>
        /// <param name="path_params">parameters for formatting the path, if it contains placeholders</param>
        /// <returns>the line read in a char array null if error occured</returns>
        public static char[] Tn_fs_readline(int line_size, string path_format, params string[] path_params)
        {
            string path;
            try
            {
                path = string.Format(path_format, path_params);
            }
            catch (System.Exception ex)
            {
                log.Debug("Cannot format the path");
                log.Debug(ex.ToString());
                return null;
            }
            FileStream file = null;
            try
            {
                file = File.OpenRead(path);
            }
            catch (System.Exception ex)
            {
                log.Debug("Cannot open the file");
                log.Debug(ex.ToString());
                CloseFile(file);
                return null;
            }

            char[] buffer = new char[line_size];
            using (StreamReader stream = new StreamReader(file))
            {
                int res = stream.Read(buffer, 0, line_size);
                if (res == 0)
                {
                    CloseFile(file);
                    return null;
                }
            }
            return buffer;
        }
        private static void CloseFile(FileStream file)
        {
            if (file != null)
            {
                file.Close();
            }
        }

    }
}