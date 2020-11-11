using MetadataExtractor;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Directory = System.IO.Directory;

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

    }

    public class MetaDataReader
    {
        private MetaDataReaderSettings m_Settings { get; }
        private List<PhotoMetaData> m_MetaData { get; set; }

        public MetaDataReader(MetaDataReaderSettings settings)
        {
            m_Settings = settings;
        }

        public void Debug(string file)
        {
            var photo = new PhotoMetaData();

            FileInfo fi = new FileInfo(file);
            photo.MetaData = ImageMetadataReader.ReadMetadata(file);

            photo.OriginalPath = file;
            photo.FileName = file.Substring(file.LastIndexOf('\\') + 1);
            photo.FileExtension = file.Substring(file.LastIndexOf('.') + 1);
            photo.DateTimeFromEXIF = DateTimeFromExif(photo.MetaData);
            photo.DateTimeFromFileName = ParseDateTimeFromFileName(photo.FileName);
            photo.DateTimeFromFileInfo = fi.LastWriteTime;
            
            photo.DateTimeTaken = photo.DateTimeFromFileName;
            if (photo.DateTimeFromEXIF > photo.DateTimeFromFileName)
                photo.DateTimeTaken = photo.DateTimeFromEXIF;
            if (photo.DateTimeFromFileInfo > photo.DateTimeFromFileName)
                photo.DateTimeTaken = photo.DateTimeFromFileInfo;

        }

        public List<PhotoMetaData> GetMetaData()
        {
            if (m_Settings == null)
                throw new Exception("MetaDataReader not initilized");

            var reply = new List<PhotoMetaData>();




            var folders = Directory.GetDirectories(m_Settings.RootFolder);
            foreach (var folder in folders)
            {
                // write to a new line
                Console.WriteLine(""); // we update this as we go.
                // skip this folder if its the output
                if (folder == m_Settings.OutputFolder)
                    continue;

                var fileCount = 0;
                foreach (string file in Directory.EnumerateFiles(folder))
                {
                    fileCount++;
                    if (fileCount < 10000)
                    {
                        var photo = new PhotoMetaData();

                        photo.MetaData = ImageMetadataReader.ReadMetadata(file);

                        photo.OriginalPath = file;
                        photo.Folder = folder.Substring(folder.LastIndexOf('\\')+1);
                        photo.FileName = file.Substring(file.LastIndexOf('\\')+1);
                        photo.FileExtension = file.Substring(file.LastIndexOf('.') + 1);
                        photo.DateTimeFromEXIF = DateTimeFromExif(photo.MetaData);
                        photo.DateTimeFromFileName = ParseDateTimeFromFileName(photo.FileName);


                        photo.DateTimeTaken = photo.DateTimeFromFileName;
                        if (photo.DateTimeFromEXIF > photo.DateTimeFromFileName)
                            photo.DateTimeTaken = photo.DateTimeFromEXIF;

                        reply.Add(photo);

                        Console.Write($"\r{photo.Folder} {fileCount} Images     ");

                    }
                    
                }
            }

            m_MetaData = reply;
            return reply;
        }

        public void CleanOutputFolder()
        {
            if (m_Settings == null)
                throw new Exception("MetaDataReader not initilized");

            System.IO.DirectoryInfo di = new DirectoryInfo(m_Settings.OutputFolder);

            Console.WriteLine("");
            Console.WriteLine($"Cleaning output folders: {m_Settings.OutputFolder}");
            Console.WriteLine(""); // to ensure we are on a new line as we overright

            if(Directory.Exists(m_Settings.OutputFolder))
                Directory.Delete(m_Settings.OutputFolder, true);
        }

        public void CopyToNewStructure()
        {
            if (m_Settings == null)
                throw new Exception("MetaDataReader not initilized");

            Console.WriteLine(""); //empty line so we know we have a new one

            Console.WriteLine("Sorting by Photo.DateTimeTaken");
            var orderedPhotos = m_MetaData.OrderBy(p => p.DateTimeTaken);

            // read through the metadata and create the folder structure
            Directory.CreateDirectory(m_Settings.OutputFolder);
            var photoCount = 0;
            foreach(var photo in orderedPhotos)
            {
                // for now skip the none photos
                if(photo.FileExtension != "jpg")
                    continue;

                if(photo.DateTimeTaken == DateTime.MinValue)
                {

                }

                photoCount++;
                Console.Write($"\rCopying image {photoCount} of {m_MetaData.Count}         ");

                var newFolder = @$"{m_Settings.OutputFolder}\{photo.DateTimeTaken.ToString("yyyy-MM-dd")}";
                Directory.CreateDirectory(newFolder);

                File.Copy(photo.OriginalPath, $@"{newFolder}\{photo.DateTimeTaken.ToString("yyyy-MM-dd_HHmmss")} ({photoCount}).{photo.FileExtension}",true);
            }
        }

        private DateTime ParseDateTimeFromFileName(string file)
        {
            var reply = new DateTime();
            // file names should be yyyyMMdd_HHmmss (hopefully)
            var regEx = new Regex(@"^20((\d){6})_((\d){6})");
            if(regEx.IsMatch(file))
            {
                DateTime.TryParseExact(file.Substring(0, 15), "yyyyMMdd_HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out reply);
            }
            // what if its a silly WhatsApp file name??
            regEx = new Regex(@"^IMG-(\d){8}-");
            if (regEx.IsMatch(file))
            {
                DateTime.TryParseExact(file.Substring(4, 8), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out reply);
            }

            return reply;
        }

        private DateTime DateTimeFromExif(IReadOnlyList<MetadataExtractor.Directory> data)
        {
            var reply = new DateTime();
            // look for the key based on the name
            foreach(var dir in data)
            {
                foreach(var tag in dir.Tags)
                {
                    if (tag.Name == m_Settings.CreateDateExifTag)
                    {
                        DateTime.TryParseExact(tag.Description, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out reply);
                        return reply;
                    }
                }
            }
            return reply;
        }
    }
}
