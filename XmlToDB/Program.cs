﻿using System;
using XmlToDB;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)

        {
            string xml = "file.xml";
            Console.Write("Введите путь к файлу: ");
            xml = Console.ReadLine();

            DBConnectionHelper dBConnectionHelper = new DBConnectionHelper();
            dBConnectionHelper.Connect(xml);

            Console.ReadLine();
        }
    }
}