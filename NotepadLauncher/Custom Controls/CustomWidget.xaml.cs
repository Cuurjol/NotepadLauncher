using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NotepadLauncher.Models;

namespace NotepadLauncher.Custom_Controls
{
    /// <summary>
    /// Логика взаимодействия для CustomWidget.xaml
    /// </summary>
    public partial class CustomWidget
    {
        public Process OpenedFile { get; set; }

        /* Ситуация: в главном окне в ListBox лежат CustomWidgets, например 4 штуки. В первых 3 пользователь сможет открыть текстовые файлы, в последнем 4 CustomWidget
         * пользователь не сможет открыть файл, поскольку файл физически может не оказаться в нужном каталоге. Соответственно, эту ситуацию нужно как-то обработать,
         * а именно: создаётся новый текстовый файл, и в зависимости от выбора действий пользователя в программе Notepad, имя текстового файла либо остенется таким же,
         * либо будет изменено на другое (и возможно расположение файла будет изменнено). Поэтому, нужно эту информацию как-то отслеживать.
         */
        private string _newFilePathInfo;

        public bool OpenFileStatus
        {
            get
            {
                Brush fill = EllipseState.Fill;
                Brush stroke = EllipseState.Stroke;

                return ((SolidColorBrush)fill).Color != Colors.Green || ((SolidColorBrush)stroke).Color != Colors.Green;
            }
            set
            {
                if (value)
                {
                    EllipseState.Fill = new SolidColorBrush(Colors.Red);
                    EllipseState.Stroke = new SolidColorBrush(Colors.Red);
                }
                else
                {
                    EllipseState.Fill = new SolidColorBrush(Colors.Green);
                    EllipseState.Stroke = new SolidColorBrush(Colors.Green);
                }
            }
        }

        public string FileName
        {
            get => TextBoxFileName.Text;
            set => TextBoxFileName.Text = value;
        }

        public bool IsEnabledTextBoxFileName
        {
            get => TextBoxFileName.IsEnabled;
            set => TextBoxFileName.IsEnabled = value;
        }

        public string FilePathInfo
        {
            get => TextBoxFilePathInfo.Text;
            set => TextBoxFilePathInfo.Text = value;
        }

        public bool IsEnabledTextBoxFilePathInfo
        {
            get => TextBoxFilePathInfo.IsEnabled;
            set => TextBoxFilePathInfo.IsEnabled = value;
        }

        public bool VisibilityButtonCloseFile
        {
            get => ButtonCloseFile.Visibility == Visibility.Visible;
            set => ButtonCloseFile.Visibility = value ? Visibility.Visible : Visibility.Hidden;
        }

        public CustomWidget()
        {
            InitializeComponent();
        }

        private void Button_FilePathInfo_Click(object sender, RoutedEventArgs e)
        {
            TextBoxFilePathInfo.Visibility = TextBoxFilePathInfo.Visibility == Visibility.Hidden ? Visibility.Visible : Visibility.Hidden;
        }

        private void Button_CloseTextFile_Click(object sender, RoutedEventArgs e)
        {
            if (OpenedFile?.HasExited == false)
            {
                OpenedFile.CloseMainWindow();
            }
        }

        private void TextBox_OpenTextFile_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            TextBoxFileName.IsEnabled = false;
            TextBoxFilePathInfo.IsEnabled = false;
            ButtonCloseFile.Visibility = Visibility.Visible;
            OpenFileStatus = true;

            //https://docs.microsoft.com/ru-ru/previous-versions/visualstudio/visual-studio-2008/ch2s8yd7(v%3dvs.90)
            //http://www.cyberforum.ru/csharp-beginners/thread1777065.html
            //https://msdn.microsoft.com/ru-ru/library/system.io.filesystemwatcher(v=vs.90).aspx

            FileSystemWatcher watcher = new FileSystemWatcher(@"C:\")
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName,
                Filter = "*.txt"
            };

            watcher.Created += OnCreated;

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo("notepad", Path.Combine(FilePathInfo, FileName)),
                EnableRaisingEvents = true
            };

            _newFilePathInfo = Path.Combine(FilePathInfo, FileName);

            process.Exited += (x, y) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (_newFilePathInfo.Equals(Path.Combine(FilePathInfo, FileName)))
                    {
                        TextBoxFileName.IsEnabled = true;
                        TextBoxFilePathInfo.IsEnabled = true;
                        ButtonCloseFile.Visibility = Visibility.Hidden;
                        OpenFileStatus = false;
                    }
                    else
                    {
                        FileInfo fileInfo = new FileInfo(_newFilePathInfo);

                        List<FileItem> fileItemsList = JsonWorker.DeserializationFileItemsJson(JsonWorker.JsonFileItems);

                        FileItem fileItem = fileItemsList.First(file => file.Name == FileName && file.Path == FilePathInfo);
                        fileItem.Name = fileInfo.Name;
                        fileItem.Path = fileInfo.DirectoryName;

                        JsonWorker.JsonFileItems = JsonWorker.SerializationFileItemsJson(fileItemsList);

                        File.WriteAllText(JsonWorker.JsonFilePath, JsonWorker.JsonFileItems);

                        FileName = fileInfo.Name;
                        FilePathInfo = fileInfo.DirectoryName;

                        TextBoxFileName.IsEnabled = true;
                        TextBoxFilePathInfo.IsEnabled = true;
                        ButtonCloseFile.Visibility = Visibility.Hidden;
                        OpenFileStatus = false;
                    }
                });
            };

            OpenedFile = process;
            process.Start();
        }

        private void TextBox_OpenExplore_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            string path = Path.Combine(FilePathInfo, FileName);

            if (File.Exists(path))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", " /select, " + path));
            }
            else
            {
                MessageBox.Show("Текстовый файл \"" + FileName + "\" не существует в директиве Explorer \"" + FilePathInfo + "\"");
            }
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            _newFilePathInfo = e.FullPath;
        }
    }
}
