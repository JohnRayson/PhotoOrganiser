using System;
using System.Collections.Generic;
using System.Text;

namespace PhotoOrganizer
{
    public class PhotoMetaData
    {
        public string Folder { get; set; }
        public string OriginalPath { get; set; }
        public string FileName { get; set; }
        public DateTime DateTimeFromFileName { get; set; }
        public DateTime DateTimeFromEXIF { get; set; }
        public DateTime DateTimeFromFileInfo { get; set; }
        public DateTime DateTimeTaken { get; set; }
        public string Photographer { get; set; }
        public string FileExtension { get; set; }
        public IReadOnlyList<MetadataExtractor.Directory> MetaData { get; set; }
    }

    public class MetaDataReaderSettings
    {
        public string RootFolder { get; set; }
        public string OutputFolder { get; set; }
        public string CreateDateExifTag { get; set; }
        public List<string> Includes { get; set; }
        public List<string> Excludes { get; set; }
        public bool AllFolders { get; set;}
        public bool WipeOutputFolder { get; set; }
        public bool RunOutput { get; set; }

        public MetaDataReaderSettings()
        {
            Includes = new List<string>();
            Excludes = new List<string>();
            AllFolders = false;
            WipeOutputFolder = true;
            RunOutput = false;
        }
    }
}
