using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Scannit.Broker
{
    public static class SharedState
    {
        private const string MutexName = "SharedFile";
        private const string SharedFileName = "SharedFile.json";
        private static Mutex _fileMutex = new Mutex(false, MutexName);

        public static SharedFileModel Get()
        {
            SharedFileModel readFile = null;
            try
            {
                _fileMutex.WaitOne(5000);
               
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Reading shared state failed. Message: {ex.Message}. Stack Trace: {ex.StackTrace}");
            }

            finally
            {
                _fileMutex.ReleaseMutex();
            }
            return readFile;
        }

        public static void Set(SharedFileModel newState)
        {
            try
            {
                _fileMutex.WaitOne(5000);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Reading shared state failed. Message: {ex.Message}. Stack Trace: {ex.StackTrace}");
            }

            finally
            {
                _fileMutex.ReleaseMutex();
            }
        }
    }
}
