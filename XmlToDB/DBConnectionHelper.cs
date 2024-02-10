using System;
using System.Data;
using System.Data.SqlClient;
using System.Configuration;
using System.Globalization;
using System.Xml;


namespace XmlToDB
{
    public class DBConnectionHelper
    {
        public void Connect(string xml)
        {
            int покупки = 0, товары = 0, пользователи = 0, деталипокупок = 0;
            //Получение строки из конфига
            string ConnectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            SqlConnection connection = new SqlConnection(ConnectionString);
            try
            {
                // Открываем подключение
                connection.Open();
                Console.WriteLine("Подключение открыто");

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(xml);

                // Получение всех элементов <order> из XML
                XmlNodeList orderNodes = xmlDoc.SelectNodes("//order");

                foreach (XmlNode orderNode in orderNodes)
                {
                    // Извлечение данных из XML для Товаров
                    XmlNodeList productNodes = orderNode.SelectNodes("product");
                    foreach (XmlNode Товар in productNodes)
                    {
                        string productName = Товар.SelectSingleNode("name").InnerText;
                        decimal price = Convert.ToDecimal(Товар.SelectSingleNode("price").InnerText, NumberFormatInfo.InvariantInfo);
                        if (CheckDBProduct(productName, connection))
                            continue;
                        // Создание записи в таблице Товары
                        string productQuery = $"INSERT INTO Товары (productName, price) VALUES (@Name, @Price)";
                        using (SqlCommand productCommand = new SqlCommand(productQuery, connection))
                        {
                            productCommand.Parameters.AddWithValue("@Name", productName);
                            productCommand.Parameters.AddWithValue("@Price", price);

                            товары += productCommand.ExecuteNonQuery();
                        }
                    }
                    // Извлечение данных из XML для Пользователи
                    XmlNodeList Пользователи = orderNode.SelectNodes("user");
                    foreach (XmlNode Пользователь in Пользователи)
                    {
                        string FIO = Пользователь.SelectSingleNode("fio").InnerText;
                        string Email = Пользователь.SelectSingleNode("email").InnerText;
                        if (CheckDBUser(FIO, connection))
                            continue;

                        // Создание записи в таблице Пользователи
                        string employeeQuery = "INSERT INTO Пользователи (FIO, Email) VALUES  (@FIO, @Email)";
                        using (SqlCommand employeeCommand = new SqlCommand(employeeQuery, connection))
                        {
                            employeeCommand.Parameters.AddWithValue("@FIO", FIO);
                            employeeCommand.Parameters.AddWithValue("@Email", Email);

                            пользователи += employeeCommand.ExecuteNonQuery();
                        }
                    }
                    // Извлечение данных из XML для Покупки
                    {
                        string OrderID = orderNode.SelectSingleNode("no").InnerText;
                        DateTime OrderDate = Convert.ToDateTime(orderNode.SelectSingleNode("reg_date").InnerText, NumberFormatInfo.InvariantInfo);
                        decimal sum = Convert.ToDecimal(orderNode.SelectSingleNode("sum").InnerText, NumberFormatInfo.InvariantInfo);

                        //Запрос ключа для пользователя
                        string FIO = orderNode.SelectSingleNode("user/fio").InnerText;
                        string EmployeeID = $"SELECT EmployeeID FROM Пользователи WHERE FIO = '{FIO}'";

                        // Создание записи в таблице Покупки
                        string orderQuery = @$"SET IDENTITY_INSERT Покупки ON
INSERT INTO Покупки (OrderID, EmployeeID, OrderDate, Sum) VALUES  (@OrderID, ({EmployeeID}), @OrderDate, @sum)";
                        using (SqlCommand employeeCommand = new SqlCommand(orderQuery, connection))
                        {
                            employeeCommand.Parameters.AddWithValue("@OrderID", OrderID);
                            employeeCommand.Parameters.AddWithValue("@OrderDate", OrderDate);
                            employeeCommand.Parameters.AddWithValue("@sum", sum);


                            покупки += employeeCommand.ExecuteNonQuery();
                        }
                    }
                    // Извлечение данных из XML для Детали покупок
                    XmlNodeList delailorder = orderNode.SelectNodes("product");
                    foreach (XmlNode покупка in delailorder)
                    {
                        int quantity = Convert.ToInt32(покупка.SelectSingleNode("quantity").InnerText);
                        
                        //Запрос ключа для Товары
                        string productName = покупка.SelectSingleNode("name").InnerText;
                        string ProductID = $"SELECT ProductID FROM Товары WHERE ProductName = '{productName}'"; 
                        
                        string OrderID = orderNode.SelectSingleNode("no").InnerText;


                        // Создание записи в таблице Покупки
                        string orderQuery = $"INSERT INTO ДеталиПокупок (OrderID, ProductID, quantity) VALUES  (@OrderID, ({ProductID}), @quantity)";
                        using (SqlCommand detailorderCommand = new SqlCommand(orderQuery, connection))
                        {
                            detailorderCommand.Parameters.AddWithValue("@OrderID", OrderID);
                            detailorderCommand.Parameters.AddWithValue("@quantity", quantity);

                            деталипокупок += detailorderCommand.ExecuteNonQuery();
                        }
                    }
                }
                Console.WriteLine($"Данные из XML файла успешно загружены в базу данных для таблицы товары. В Количестве: {товары}");
                Console.WriteLine($"Данные из XML файла успешно загружены в базу данных для таблицы пользователи. В Количестве: {пользователи}");
                Console.WriteLine($"Данные из XML файла успешно загружены в базу данных для таблицы покупки. В Количестве: {покупки}");
                Console.WriteLine($"Данные из XML файла успешно загружены в базу данных для таблицы деталипокупок. В Количестве: {деталипокупок}");
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
        }
        private bool CheckDBProduct(string productName, SqlConnection connection)
        {
            string query = "SELECT COUNT(*) FROM Товары WHERE ProductName = @productName";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@productName", productName);

                int count = (int)command.ExecuteScalar();

                if (count == 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        private bool CheckDBUser(string user, SqlConnection connection)
        {
            string query = "SELECT COUNT(*) FROM Пользователи WHERE FIO = @user";
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@user", user);

                int count = (int)command.ExecuteScalar();

                if (count == 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
    }
}