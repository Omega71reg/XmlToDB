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
            // Открываем подключение
            connection.Open();
            // Начинаем транзакцию
            SqlTransaction transaction = connection.BeginTransaction(); ;
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(xml);

                // Получение всех элементов <order> из XML
                XmlNodeList orderNodes = xmlDoc.SelectNodes("//order");

                foreach (XmlNode orderNode in orderNodes)
                {
                    string Order = orderNode.SelectSingleNode("no").InnerText;
                    //Проверяем на корректность полей заказа
                    if (!CheckXMLForCorrectness(orderNode))
                    {
                        Console.WriteLine($"Для заказа №{Order} не заполнено одно из полей, Запись невозможна.");
                        continue;
                    }
                    //Проверяем на существующий заказ в БД
                    if (CheckDBOrder(Order, connection, transaction))
                    {
                        continue;
                    }
                    // Извлечение данных из XML для Товаров
                    XmlNodeList productNodes = orderNode.SelectNodes("product");
                    foreach (XmlNode Товар in productNodes)
                    {
                        string productName = Товар.SelectSingleNode("name").InnerText;
                        decimal price = Convert.ToDecimal(Товар.SelectSingleNode("price").InnerText, NumberFormatInfo.InvariantInfo);
                        //Проверка есть ли в БД товар
                        if (CheckDBProduct(productName, connection, transaction))
                            continue;
                        // Создание записи в таблице Товары
                        string productQuery = $"INSERT INTO Товары (productName, price) VALUES (@Name, @Price)";
                        using (SqlCommand productCommand = new SqlCommand(productQuery, connection, transaction))
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
                        //Проверка есть ли в БД пользователь
                        if (CheckDBUser(FIO, connection, transaction))
                            continue;

                        // Создание записи в таблице Пользователи
                        string employeeQuery = "INSERT INTO Пользователи (FIO, Email) VALUES  (@FIO, @Email)";
                        using (SqlCommand employeeCommand = new SqlCommand(employeeQuery, connection, transaction))
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
                        using (SqlCommand employeeCommand = new SqlCommand(orderQuery, connection, transaction))
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
                        using (SqlCommand detailorderCommand = new SqlCommand(orderQuery, connection, transaction))
                        {
                            detailorderCommand.Parameters.AddWithValue("@OrderID", OrderID);
                            detailorderCommand.Parameters.AddWithValue("@quantity", quantity);

                            деталипокупок += detailorderCommand.ExecuteNonQuery();
                        }
                    }
                    // Подтвердить транзакцию
                    transaction.Commit();
                }
                Console.WriteLine($"Данные из XML файла успешно загружены в базу данных для таблицы товары. В Количестве: {товары}");
                Console.WriteLine($"Данные из XML файла успешно загружены в базу данных для таблицы пользователи. В Количестве: {пользователи}");
                Console.WriteLine($"Данные из XML файла успешно загружены в базу данных для таблицы покупки. В Количестве: {покупки}");
                Console.WriteLine($"Данные из XML файла успешно загружены в базу данных для таблицы деталипокупок. В Количестве: {деталипокупок}");
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Ошибка подключения: {ex.Message}");
                // Откатить транзакцию в случае ошибки
                transaction.Rollback();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                // Откатить транзакцию в случае ошибки
                transaction.Rollback();
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
        private bool CheckDBProduct(string productName, SqlConnection connection, SqlTransaction transaction)
        {
            string query = "SELECT COUNT(*) FROM Товары WHERE ProductName = @productName";
            using (SqlCommand command = new SqlCommand(query, connection, transaction))
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
        private bool CheckDBUser(string user, SqlConnection connection, SqlTransaction transaction)
        {
            string query = "SELECT COUNT(*) FROM Пользователи WHERE FIO = @user";
            using (SqlCommand command = new SqlCommand(query, connection, transaction))
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
        private bool CheckDBOrder(string order, SqlConnection connection, SqlTransaction transaction)
        {
            string query = "SELECT COUNT(*) FROM Покупки WHERE OrderID = @order";
            using (SqlCommand command = new SqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@order", order);

                int count = (int)command.ExecuteScalar();

                if (count == 0)
                {
                    return false;
                }
                else
                {
                    Console.WriteLine($"Данный заказ '{order}' существует в БД. Пропускаем запись.");
                    return true;
                }
            }
        }
        private bool CheckXMLForCorrectness(XmlNode orderNode)
        {
            bool flag = false;
            int orderNo;
            if (Int32.TryParse(orderNode.SelectSingleNode("no")?.InnerText, out orderNo))
            {
                {
                    DateTime regDate;
                    if (DateTime.TryParseExact(orderNode.SelectSingleNode("reg_date")?.InnerText, "yyyy.MM.dd", null, System.Globalization.DateTimeStyles.None, out regDate))
                    {
                        decimal sum;
                        if (decimal.TryParse(Convert.ToDecimal(orderNode.SelectSingleNode("sum").InnerText, NumberFormatInfo.InvariantInfo).ToString(), out sum))
                        {
                            string fio = orderNode.SelectSingleNode("user/fio")?.InnerText;
                            string email = orderNode.SelectSingleNode("user/email")?.InnerText;

                            if (!string.IsNullOrEmpty(fio) && !string.IsNullOrEmpty(email))
                            {
                                XmlNodeList productNodes = orderNode.SelectNodes("product");
                                foreach (XmlNode Товар in productNodes)
                                {
                                    string quantity = "";
                                    string name = "";
                                    string price = "";
                                    flag = false;
                                    quantity = Товар.SelectSingleNode("quantity")?.InnerText;
                                    name = Товар.SelectSingleNode("name")?.InnerText;
                                    price = Товар.SelectSingleNode("price")?.InnerText;
                                    bool temp = (!string.IsNullOrEmpty(quantity) && !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(price));
                                    if (temp)
                                    {
                                        flag = true;
                                    }
                                    if (!temp) break;
                                }
                            }
                        }
                    }
                }
            }
            return flag;
        }
    }
}