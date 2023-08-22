using System.IO;
using System.Reflection;
using System;
using System.Globalization;
using OpenVRInputTest;
namespace LogWriterTest {
    public static class LogWriter {
        private static string m_exePath = string.Empty;
        public static void LogWrite(string logMessage, string filename) {
            m_exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!File.Exists(m_exePath + "\\" + filename))
                File.Create(m_exePath + "\\" + filename);
            try {
                using (StreamWriter w = File.AppendText(m_exePath + "\\" + filename))
                    AppendLog(logMessage, w);
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }

        }

        private static void AppendLog(string logMessage, TextWriter txtWriter) {
            try {
                if (logMessage == "\n") {
                    txtWriter.WriteLine();
                }
                else {
                    //CultureInfo ci = new CultureInfo("en-US");
                    //txtWriter.Write("{0}", DateTime.Now.ToString("MM/dd HH:mm:ss.fff", ci));
                    //txtWriter.Write("{0}", DateTime.Now.ToString("MM/dd HH:mm:ss", ci));
                    //txtWriter.WriteLine("  :{0}", logMessage);
                    txtWriter.WriteLine(logMessage);
                    txtWriter.Close();
                }

            }
            catch (Exception e) {
                Utils.PrintWarning($"Log write error: {e.Message}");
            }
        }
    }
}
