﻿using MetadataExtractor;
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

                var settings = new MetaDataReaderSettings();

                settings.RootFolder = @"D:\Photos\Hettie";            
                settings.OutputFolder = $@"{settings.RootFolder}\57days";
                settings.CreateDateExifTag = "Date/Time Original";

                var reader = new MetaDataReader(settings);

                reader.Debug($@"{settings.RootFolder}\Marie Phone\IMG-20201017-WA0006.jpg");

                var photos = reader.GetMetaData();
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
