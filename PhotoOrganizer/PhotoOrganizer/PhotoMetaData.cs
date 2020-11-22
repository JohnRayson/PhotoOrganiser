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
    public class MetaDataReader
    {
        private MetaDataReaderSettings m_Settings { get; }
        private List<PhotoMetaData> m_MetaData { get; set; }
        private string m_LogFile { get; set; }

        public MetaDataReader(MetaDataReaderSettings settings)
        {
            m_Settings = settings;
            var systemPath = System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var path = Path.Combine(systemPath, $@"PhotoOrganiser\Logs\");
            Directory.CreateDirectory(path);
            m_LogFile = $"{path}{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}.txt";
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
            WriteToLogFile("GetMetaData()");
            if (m_Settings == null)
            {
                var msg = "MetaDataReader not initilized";
                WriteToLogFile(msg);
                Console.WriteLine("");
                Console.WriteLine(msg);
                throw new Exception(msg);
            }
                

            var reply = new List<PhotoMetaData>();

            var folders = Directory.GetDirectories(m_Settings.RootFolder);
            foreach (var folder in folders)
            {
                // skip this folder if its the output
                if (folder == m_Settings.OutputFolder)
                    continue;

                // work out if we want to include this folder
                bool process = m_Settings.AllFolders;
                var folderName = folder.Replace(m_Settings.RootFolder, "");

                // are we explicitly excluding it
                if (!process)
                {
                    foreach (string dir in m_Settings.Includes)
                    {
                        if (dir == folderName)
                            process = true; 
                    }
                }

                // are we explicitly excluding it
                foreach (string dir in m_Settings.Excludes)
                {
                    if (dir == folderName)
                        process = false;
                }

                if (!process)
                    continue;

                // processing this folder
                // write to a new line
                Console.WriteLine(""); // we update this as we go.

                var errorList = new List<Exception>();

                var fileCount = 0;
                foreach (string file in Directory.EnumerateFiles(folder))
                {
                    fileCount++;
                    if (fileCount < 10000)
                    {
                        try
                        {
                            var photo = new PhotoMetaData();

                            photo.MetaData = ImageMetadataReader.ReadMetadata(file);

                            photo.OriginalPath = file;
                            photo.Folder = folder.Substring(folder.LastIndexOf('\\') + 1);
                            photo.FileName = file.Substring(file.LastIndexOf('\\') + 1);
                            photo.FileExtension = file.Substring(file.LastIndexOf('.') + 1);
                            photo.DateTimeFromEXIF = DateTimeFromExif(photo.MetaData);
                            photo.DateTimeFromFileName = ParseDateTimeFromFileName(photo.FileName);


                            photo.DateTimeTaken = photo.DateTimeFromFileName;
                            if (photo.DateTimeFromEXIF > photo.DateTimeFromFileName)
                                photo.DateTimeTaken = photo.DateTimeFromEXIF;

                            reply.Add(photo);

                            Console.Write($"\r{photo.Folder} {fileCount} Images     ");
                        }
                        catch(Exception ex)
                        {
                            errorList.Add(ex);
                        }
                    }
                }
                Console.WriteLine("");
                Console.WriteLine($"{folder} had {errorList.Count} files which could not be read");

                WriteToLogFile($"{folder} had {fileCount} Images");
                WriteToLogFile($"{folder} had {errorList.Count} files which could not be read");
            }

            m_MetaData = reply;
            return reply;
        }

        public void CleanOutputFolder()
        {
            WriteToLogFile("CleanOutputFolder()");
            if (m_Settings == null)
            {
                var msg = "MetaDataReader not initilized";
                WriteToLogFile(msg);
                Console.WriteLine("");
                Console.WriteLine(msg);
                throw new Exception(msg);
            }

            System.IO.DirectoryInfo di = new DirectoryInfo(m_Settings.OutputFolder);

            Console.WriteLine("");
            Console.WriteLine($"Cleaning output folders: {m_Settings.OutputFolder}");
            Console.WriteLine(""); // to ensure we are on a new line as we overright

            if(Directory.Exists(m_Settings.OutputFolder))
                Directory.Delete(m_Settings.OutputFolder, true);
        }

        public void CopyToNewStructure()
        {
            WriteToLogFile("CopyToNewStructure()");
            if (m_Settings == null)
            {
                var msg = "MetaDataReader not initilized";
                WriteToLogFile(msg);
                Console.WriteLine("");
                Console.WriteLine(msg);
                throw new Exception(msg);
            }
            
            Console.WriteLine(""); //empty line so we know we have a new one

            Console.WriteLine("Sorting by Photo.DateTimeTaken");
            var orderedPhotos = m_MetaData.OrderBy(p => p.DateTimeTaken);

            // read through the metadata and create the folder structure
            Directory.CreateDirectory(m_Settings.OutputFolder);
            var photoCount = 0;
            var errorList = new List<Exception>();
            foreach(var photo in orderedPhotos)
            {
                photoCount++;
                Console.Write($"\rCopying image {photoCount} of {m_MetaData.Count}         ");

                var newFolder = @$"{m_Settings.OutputFolder}\{photo.DateTimeTaken.ToString("yyyy-MM-dd")}";
                Directory.CreateDirectory(newFolder);

                try
                {
                    var newFileName = $@"{newFolder}\{photo.DateTimeTaken.ToString("yyyy-MM-dd_HHmmss")} ({photoCount}).{photo.FileExtension}";
                    WriteToLogFile($"From: {photo.OriginalPath} to: {newFileName}");
                    
                    if(m_Settings.RunOutput)
                        File.Copy(photo.OriginalPath, newFileName, true);
                }
                catch(Exception ex)
                {
                    errorList.Add(ex);
                }
            }
            Console.WriteLine("");
            Console.WriteLine($"Failed to copy {errorList.Count} photos");

            WriteToLogFile($"{photoCount} Images");
            WriteToLogFile($"{errorList.Count} files which could not be copied");
        }

        private void WriteToLogFile(string message)
        {
            using (StreamWriter fs = new StreamWriter(m_LogFile,true))
            {
                fs.Write($"[{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}]: {message}{Environment.NewLine}");
            }
        }

        private DateTime ParseDateTimeFromFileName(string file)
        {
            var reply = new DateTime();
            // strip some starting info off the file name
            file = file.Replace("VID", "");
            file = file.Replace("IMG", "");
            file = file.Replace("_", "");
            file = file.Replace("-", "");


            // file names should be yyyyMMdd_HHmmss (hopefully)
            var regEx = new Regex(@"^20((\d){6})((\d){6})");
            if(regEx.IsMatch(file))
            {
                DateTime.TryParseExact(file.Substring(0, 14), "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out reply);
            }
            // what if its a silly WhatsApp file name??
            regEx = new Regex(@"^(\d){8}");
            if (regEx.IsMatch(file))
            {
                DateTime.TryParseExact(file.Substring(0, 8), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out reply);
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
