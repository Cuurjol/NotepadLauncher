using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using NotepadLauncher.Models;

namespace NotepadLauncher
{
    public static class JsonWorker
    {
        public static string JsonFileItems { get; set; }
        public static string JsonFilePath { get; set; }

        static JsonWorker()
        {
            JsonFilePath = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory?.FullName + @"\FileItems.json";
        }

        public static void CreateJsonFile(List<FileItem> fileItemsList)
        {
            File.Create(JsonFilePath).Dispose();

            JsonFileItems = SerializationFileItemsJson(fileItemsList);

            File.WriteAllText(JsonFilePath, JsonFileItems);
        }

        public static string SerializationFileItemsJson(List<FileItem> fileItemsList)
        {
            return JsonConvert.SerializeObject(fileItemsList.ToArray());
        }

        public static List<FileItem> DeserializationFileItemsJson(string json)
        {
            return JsonConvert.DeserializeObject<List<FileItem>>(json);
        }
    }
}
