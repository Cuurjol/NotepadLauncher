using System.Runtime.Serialization;

namespace NotepadLauncher.Models
{
    [DataContract]
    public class FileItem
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Path { get; set; }
    }
}
