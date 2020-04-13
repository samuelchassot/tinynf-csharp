using Utilities;
using System.IO;

namespace Env.linuxx86
{
    /// <summary>
    /// Abstracts the linux-x86 file system
    /// </summary>
    public static class Filesystem
    {
        /// <summary>
        /// Reads a single line from the file at the given path into the given line, reading at most line_size characters.
        /// The file can have c#-style placeholders ("this is an example of placeholders {0} with another one here {1}"), in which case additional arguments must be passed.
        /// </summary>
        /// <param name="lineSize">the size of the line to read at most</param>
        /// <param name="pathFormat">the path of the file to read, can contains fprintf style placeholders</param>
        /// <param name="pathParams">parameters for formatting the path, if it contains placeholders</param>
        /// <returns>the line read in a char array null if error occured</returns>
        public static char[] TnFsReadline(int lineSize, string pathFormat, params string[] pathParams)
        {
            string path;
            try
            {
                path = string.Format(pathFormat, pathParams);
            }
            catch (System.Exception ex)
            {
                Util.log.Debug("Cannot format the path");
                Util.log.Debug(ex.ToString());
                return null;
            }
            FileStream file = null;
            try
            {
                file = File.OpenRead(path);
            }
            catch (System.Exception ex)
            {
                Util.log.Debug("Cannot open the file");
                Util.log.Debug(ex.ToString());
                CloseFile(file);
                return null;
            }

            char[] buffer = new char[lineSize];
            using (StreamReader stream = new StreamReader(file))
            {
                int res = stream.Read(buffer, 0, lineSize);
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