using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Globalization;
using System.Xml;


namespace XmlToDB
{
    public static class DBConnectionHelper
    {
        public static void Connect(string xml)
        {
            int temp = 0;
            //Получение строки из конфига
            string s = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            SqlConnection connection = new SqlConnection(s);
                try
                {
                    // Открываем подключение
                    connection.Open();
                    Console.WriteLine("Подключение открыто");
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(xml);

                    // Получение всех элементов <order> из XML
                    XmlNodeList orderNodes = xmlDoc.SelectNodes("//order");
                    #region Обход каждого элемента <order> и сохранение данных в базе данных
                    foreach (XmlNode orderNode in orderNodes)
                    {
                        // Извлечение данных из XML
                        string orderNo = orderNode.SelectSingleNode("no").InnerText;
                        string regDate = orderNode.SelectSingleNode("reg_date").InnerText;
                        decimal sum = Convert.ToDecimal(orderNode.SelectSingleNode("sum").InnerText, NumberFormatInfo.InvariantInfo);
                        string fio = orderNode.SelectSingleNode("user/fio").InnerText;
                        string email = orderNode.SelectSingleNode("user/email").InnerText;

                    // Создание записи в таблице ДеталиПокупок
                    string query = "INSERT INTO ДеталиПокупок (OrderID, RegDate, Sum, FIO, Email) VALUES (@OrderID, @RegDate, @Sum, @FIO, @Email)";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@OrderID", orderNo);
                            command.Parameters.AddWithValue("@RegDate", regDate);
                            command.Parameters.AddWithValue("@Sum", sum);
                            command.Parameters.AddWithValue("@FIO", fio);
                            command.Parameters.AddWithValue("@Email", email);

                            temp = command.ExecuteNonQuery();
                        }
                    }
                    #endregion
                }
                catch (SqlException ex)
                {
                    Console.WriteLine($"Ошибка подключения: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                }
                finally
                {
                    // если подключение открыто
                    if (connection.State == ConnectionState.Open)
                    {
                        // закрываем подключение
                        connection.Close();
                        Console.WriteLine("Подключение закрыто...");
                    }
                }
                Console.WriteLine($"Данные из XML файла успешно загружены в базу данных.В Количестве: {temp}");
        }
    }
}