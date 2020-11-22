using MetadataExtractor;
using System;
using System.Collections.Generic;
using System.IO;


namespace PhotoOrganizer
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Hello World!");
                Console.WriteLine("Photo processor, to organise them in folder and provide consistent names.");

                var settings = new MetaDataReaderSettings();

                foreach(var line in System.IO.File.ReadAllLines("Settings.Conf"))
                {
                    var parts = line.Split(new char[] { '=' });
                    if (parts.Length < 2)
                        continue;

                    switch(parts[0])
                    {
                        case "RootFolder": settings.RootFolder = parts[1]; break;
                        case "OutputFolder": settings.OutputFolder = parts[1]; break;
                        case "CreateDateExifTag": settings.CreateDateExifTag = parts[1]; break;
                        case "RunOutput": settings.RunOutput = parts[1]=="true"; break;
                        case "AllFolders": settings.AllFolders = parts[1]=="true"; break;
                        case "Includes": settings.Includes = new List<string>(parts[1].Split(new char[] { ',' })); break;
                        case "Excludes": settings.Excludes = new List<string>(parts[1].Split(new char[] { ',' })); break;
                    }
                }

                var reader = new MetaDataReader(settings);

                reader.Debug($@"D:\Photos\Hettie\John\VID_20201014_134620.mp4");

                var photos = reader.GetMetaData();
                
                if(settings.WipeOutputFolder)
                    reader.CleanOutputFolder();

                reader.CopyToNewStructure();


            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            //var img = @"John Phone\20200820_070127.jpg";
            
        }
    }
}
