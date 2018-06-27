using System;
using System.Collections.Generic;
using System.Windows;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using NotepadLauncher.Custom_Controls;
using NotepadLauncher.Models;

namespace NotepadLauncher
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly string _workingDirectory = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory?.FullName;

        public MainWindow()
        {
            InitializeComponent();

            if (File.Exists(JsonWorker.JsonFilePath))
            {
                using (FileStream stream = File.OpenRead(JsonWorker.JsonFilePath))
                {
                    byte[] array = new byte[stream.Length];
                    stream.Read(array, 0, array.Length);
                    JsonWorker.JsonFileItems = Encoding.Default.GetString(array);
                }

                List<FileItem> fileItemsListFromJson = JsonWorker.DeserializationFileItemsJson(JsonWorker.JsonFileItems);

                // Если нужно, чтобы WPF-приложение загружало в ListBox пользовательские виджеты из JSON-файла FileItems.json, то оставляем код, как сейчас он есть.
                AddInListBoxCustomWidget(fileItemsListFromJson);

                /* Если нужно, чтобы WPF-приложение загружало в ListBox пользовательские виджеты из всех существующих каталогов, которые находятся в папке Debug (например, сейчас там находится 
                   одна папка Text files), то строчку кода над этим комментарием заносим под комментарий, внизу строчки кода убираем из под комментария. В данной ситуации предусмотрено, что
                   WPF-приложение может быть запущено с любого компьютера или с одного компьютера, но само приложение может быть запущено с разных каталогов файловой системы, соответственно, 
                   если WPF-приложение было в первый раз запущено с одного компьютера, а во второй раз с другого компьютера, то пути расположений файлов будут отличаться между одним компьютером 
                   и другим компьютером (или между одним каталогом и другим каталогом одного компьютера).*/

                /*List<FileItem> fileItemsListFromFolder = FileItemsInitializer(_workingDirectory);

                if (ListsEqual(fileItemsListFromJson, fileItemsListFromFolder))
                {
                    AddInListBoxCustomWidget(fileItemsListFromJson);
                }
                else
                {
                    File.Delete(JsonWorker.JsonFilePath);

                    JsonWorker.CreateJsonFile(fileItemsListFromFolder);

                    AddInListBoxCustomWidget(fileItemsListFromFolder);
                }*/
            }
            else
            {
                List<FileItem> fileItemsList = FileItemsInitializer(_workingDirectory);

                JsonWorker.CreateJsonFile(fileItemsList);

                AddInListBoxCustomWidget(fileItemsList);
            }
        }

        private void ListBox_SelectedItem_Click(object sender, SelectionChangedEventArgs e)
        {
            ButtonDeleteWidgetItem.IsEnabled = ListBoxFiles.SelectedItem != null && ((CustomWidget)ListBoxFiles.SelectedItem).OpenFileStatus == false;
        }

        private void ListBox_CancelSelection_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListBoxFiles.SelectedItem = null;
        }

        private void Button_CreateNewWidget_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog()
            {
                Filter = "Текстовые файлы (*.txt)|*.txt",
            };

            if (fileDialog.ShowDialog() == true)
            {
                FileInfo fileInfo = new FileInfo(fileDialog.FileName);

                FileItem fileItem = new FileItem()
                {
                    Name = fileInfo.Name,
                    Path = fileInfo.DirectoryName
                };

                List<FileItem> fileItemsList = JsonWorker.DeserializationFileItemsJson(JsonWorker.JsonFileItems);
                fileItemsList.Add(fileItem);

                JsonWorker.JsonFileItems = JsonWorker.SerializationFileItemsJson(fileItemsList);

                using (FileStream stream = new FileStream(JsonWorker.JsonFilePath, FileMode.OpenOrCreate))
                {
                    byte[] array = Encoding.Default.GetBytes(JsonWorker.JsonFileItems);
                    stream.Write(array, 0, array.Length);
                }

                CustomWidget newWidget = new CustomWidget()
                {
                    FileName = fileItem.Name,
                    FilePathInfo = fileItem.Path,
                    Width = 400
                };

                ListBoxFiles.Items.Add(newWidget);

                newWidget.IsEnabledTextBoxFileName = false;
                newWidget.IsEnabledTextBoxFilePathInfo = false;
                newWidget.VisibilityButtonCloseFile = true;
                newWidget.OpenFileStatus = true;

                Process process = new Process
                {
                    StartInfo = new ProcessStartInfo("notepad", Path.Combine(fileItem.Path, fileItem.Name)),
                    EnableRaisingEvents = true
                };

                process.Exited += (x, y) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        newWidget.IsEnabledTextBoxFileName = true;
                        newWidget.IsEnabledTextBoxFilePathInfo = true;
                        newWidget.VisibilityButtonCloseFile = false;
                        newWidget.OpenFileStatus = false;
                    });
                };

                newWidget.OpenedFile = process;
                process.Start();
            }
        }

        private void Button_DeleteWidget_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxFiles.SelectedItem != null)
            {
                string fileName = ((CustomWidget)ListBoxFiles.SelectedItem).FileName;
                string filePathInfo = ((CustomWidget)ListBoxFiles.SelectedItem).FilePathInfo;

                List<FileItem> fileItemsList = JsonWorker.DeserializationFileItemsJson(JsonWorker.JsonFileItems);
                FileItem fileItem = fileItemsList.First(x => x.Name == fileName && x.Path == filePathInfo);
                fileItemsList.Remove(fileItem);

                JsonWorker.JsonFileItems = JsonWorker.SerializationFileItemsJson(fileItemsList);

                File.WriteAllText(JsonWorker.JsonFilePath, JsonWorker.JsonFileItems);

                ListBoxFiles.Items.Remove(ListBoxFiles.SelectedItem);
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Process[] processes = Process.GetProcessesByName("notepad");
            if (processes.Length != 0)
            {
                foreach (Process process in processes)
                {
                    process.Kill();
                }

                Close();
            }
            else
            {
                Close();
            }
        }

        private void AddInListBoxCustomWidget(List<FileItem> fileItemsList)
        {
            foreach (FileItem file in fileItemsList)
            {
                ListBoxFiles.Items.Add(new CustomWidget
                {
                    FileName = file.Name,
                    FilePathInfo = file.Path,
                    OpenFileStatus = false,
                    Width = 400
                });
            }
        }

        private static List<FileItem> FileItemsInitializer(string directory)
        {
            string[] dirs = Directory.GetDirectories(directory);
            List<FileItem> fileItemsList = new List<FileItem>();

            foreach (string dir in dirs)
            {
                IEnumerable<string> resultFilePaths = Directory.EnumerateFiles(dir, "*.txt");

                foreach (string filePath in resultFilePaths)
                {
                    FileInfo fileInfo = new FileInfo(filePath);

                    fileItemsList.Add(new FileItem()
                    {
                        Name = fileInfo.Name,
                        Path = fileInfo.DirectoryName
                    });
                }
            }

            return fileItemsList;
        }

        private static bool ListsEqual(List<FileItem> target, List<FileItem> source)
        {
            if (target.Count != source.Count)
            {
                return false;
            }

            return !target.Where((t, i) => !t.Equals(source[i])).Any();
        }
    }
}