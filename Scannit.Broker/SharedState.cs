using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Scannit.Broker
{
    public static class SharedState
    {
        private const string MutexName = "SharedFile";        
        private static Mutex _fileMutex = new Mutex(false, MutexName);        

        public static async Task<T> GetAsync<T>(string propertyName)
        {
            T readObject = default(T);
            string fileContents = null;
            bool lockObtained = false;
            try
            {
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///{SharedFileName}"));

                lockObtained = _fileMutex.WaitOne(5000);

                Task<string> fileContentsTask = FileIO.ReadTextAsync(file).AsTask();
                fileContentsTask.Wait();
                fileContents = fileContentsTask.Result;
               
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Reading shared state failed. Message: {ex.Message}. Stack Trace: {ex.StackTrace}");
            }

            finally
            {
                if (lockObtained)
                {
                    _fileMutex.ReleaseMutex();
                }
            }

            if (fileContents != null)
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(fileContents);
                if (dict.TryGetValue(key, out object obj))
                {
                    readObject = (T)obj;
                }
            }
            return readObject;
        }        

        public static async Task SetAsync<T>(string propertyName, T valueToSet)
        {
            bool lockObtained = false;
            try
            {
                string jsonToWrite = JsonConvert.SerializeObject(valueToSet);
                StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///{SharedFileName}"));

                lockObtained = _fileMutex.WaitOne(5000);

                FileIO.WriteTextAsync(file, jsonToWrite).AsTask().Wait();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Reading shared state failed. Message: {ex.Message}. Stack Trace: {ex.StackTrace}");
            }

            finally
            {
                if (lockObtained)
                {
                    _fileMutex.ReleaseMutex();
                }
            }
        }
    }
}
