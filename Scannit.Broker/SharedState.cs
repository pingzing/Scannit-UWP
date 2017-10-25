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
        private const string LogFileName = "log.txt";
        private static readonly Dictionary<string, Mutex> _namedMutexes = new Dictionary<string, Mutex>();       

        public static async Task<T> GetAsync<T>(string propertyName)
        {
            Mutex fileMutex = _namedMutexes.GetOrAdd(propertyName, () => new Mutex(false, propertyName));
            T readObject = default(T);
            string fileContents = null;
            bool lockObtained = false;
            await LogAsync($"Attempting to retrieve shared state at {propertyName}. ");
            try
            {
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                StorageFile file = await folder.CreateFileAsync($"{propertyName}.json", CreationCollisionOption.OpenIfExists);                

                lockObtained = fileMutex.WaitOne(5000);

                if (lockObtained)
                {
                    Task<string> fileContentsTask = FileIO.ReadTextAsync(file).AsTask();
                    fileContentsTask.Wait();
                    fileContents = fileContentsTask.Result;
                }
                else
                {
                    await LogAsync($"Failed to obtain lock when attempting to read shared state ({propertyName}.");
                }
               
            }
            catch (Exception ex)
            {
                LogAsync($"Reading shared state ({propertyName}) failed. Message: {ex.Message}. Stack Trace: {ex.StackTrace}").Wait();
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
            await LogAsync($"Attempting to set shared state at {propertyName} to {valueToSet}. ");
            try
            {
                string jsonToWrite = JsonConvert.SerializeObject(valueToSet);
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                StorageFile file = await folder.CreateFileAsync($"{propertyName}.json", CreationCollisionOption.OpenIfExists);

                lockObtained = fileMutex.WaitOne(5000);

                if (lockObtained)
                {
                    FileIO.WriteTextAsync(file, jsonToWrite).AsTask().Wait();
                }
                else
                {
                    await LogAsync($"Failed to obtain lock when attempting to set shared state at {propertyName} to {valueToSet}.");
                }
            }
            catch (Exception ex)
            {
                LogAsync($"Setting shared state ({propertyName}) failed. Message: {ex.Message}. Stack Trace: {ex.StackTrace}").Wait();
            }

            finally
            {
                if (lockObtained)
                {
                    fileMutex.ReleaseMutex();
                }
            }
        }

        public static async Task LogAsync(string logLine)
        {
            Mutex logFileMutex = _namedMutexes.GetOrAdd(LogFileName, () => new Mutex(false, LogFileName));
            bool lockObtained = false;
            try
            {
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                StorageFile logFile = await folder.CreateFileAsync(LogFileName, CreationCollisionOption.OpenIfExists);

                lockObtained = logFileMutex.WaitOne(5000);
                if (lockObtained)
                {
                    DateTime now = DateTime.UtcNow;
                    FileIO.AppendTextAsync(logFile, $"{now.ToString("yyyy-MM-dd HH:mm:ss.ffff")}: {logLine}\n").AsTask().Wait();
                }
                else
                {
                    Debug.WriteLine("Failed to obtain lock when attempting to write to log.");
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Writing to log file failed. Message: {ex.Message}. Stack trace: {ex.StackTrace}");
            }
            finally
            {
                if (lockObtained)
                {
                    logFileMutex.ReleaseMutex();
                }
            }
        }

        public static async Task ClearLogAsync()
        {
            Mutex logFileMutex = _namedMutexes.GetOrAdd(LogFileName, () => new Mutex(false, LogFileName));
            bool lockObtained = false;
            try
            {
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                StorageFile logFile = await folder.CreateFileAsync(LogFileName, CreationCollisionOption.OpenIfExists);

                lockObtained = logFileMutex.WaitOne(5000);
                if (lockObtained)
                {
                    FileIO.WriteTextAsync(logFile, "").AsTask().Wait();
                }
                else
                {
                    Debug.WriteLine("Failed to obtain lock when attempting to delete log.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Deleting log file failed. Message: {ex.Message}. Stack trace: {ex.StackTrace}");
            }
            finally
            {
                logFileMutex.ReleaseMutex();
            }
        }

        public static async Task<string> GetLogContentsAsync()
        {
            Mutex logFileMutex = _namedMutexes.GetOrAdd(LogFileName, () => new Mutex(false, LogFileName));
            bool lockObtained = false;
            string logContents = "";
            try
            {
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                StorageFile logFile = await folder.CreateFileAsync(LogFileName, CreationCollisionOption.OpenIfExists);

                lockObtained = logFileMutex.WaitOne(5000);

                if (lockObtained)
                {
                    logContents = FileIO.ReadTextAsync(logFile).AsTask().Result;
                }
                else
                {
                    Debug.WriteLine("Failed to obtain lock when attempting to read log.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Reading from log file failed. Message: {ex.Message}. Stack trace: {ex.StackTrace}");
            }
            finally
            {
                if (lockObtained)
                {
                    logFileMutex.ReleaseMutex();
                }
            }
            return logContents;
        }

        // Property names
        public const string IsApplicationInForeground = nameof(IsApplicationInForeground);
        public const string LastSeenCard = nameof(LastSeenCard);
        public const string LastSeenTimestamp = nameof(LastSeenTimestamp);

    }
}
