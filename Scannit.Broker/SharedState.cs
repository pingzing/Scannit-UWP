using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Scannit.Broker
{
    public static class SharedState
    {
        private const string MutexName = "SharedFile";
        private static readonly Dictionary<string, Mutex> _namedMutexes = new Dictionary<string, Mutex>();       

        public static async Task<T> GetAsync<T>(string propertyName)
        {
            Mutex fileMutex = _namedMutexes.GetOrAdd(propertyName, () => new Mutex(false, propertyName));
            T readObject = default(T);
            string fileContents = null;
            bool lockObtained = false;
            try
            {
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                StorageFile file = await folder.CreateFileAsync($"{propertyName}.json", CreationCollisionOption.OpenIfExists);                

                lockObtained = fileMutex.WaitOne(5000);

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
                    fileMutex.ReleaseMutex();
                }
            }

            if (fileContents != null)
            {
                if (String.IsNullOrWhiteSpace(fileContents))
                {
                    readObject = default(T);
                }
                else
                {
                    readObject = JsonConvert.DeserializeObject<T>(fileContents);
                }
            }
            return readObject;
        }        

        public static async Task SetAsync<T>(string propertyName, T valueToSet)
        {
            Mutex fileMutex = _namedMutexes.GetOrAdd(propertyName, () => new Mutex(false, propertyName));
            bool lockObtained = false;
            try
            {
                string jsonToWrite = JsonConvert.SerializeObject(valueToSet);
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                StorageFile file = await folder.CreateFileAsync($"{propertyName}.json", CreationCollisionOption.OpenIfExists);

                lockObtained = fileMutex.WaitOne(5000);

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
                    fileMutex.ReleaseMutex();
                }
            }
        }

        // Property names
        public const string IsApplicationInForeground = nameof(IsApplicationInForeground);
        public const string LastSeenCard = nameof(LastSeenCard);
        public const string LastSeenTimestamp = nameof(LastSeenTimestamp);

    }
}
