using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Turfirm.Infrastructure;

namespace Turfirm.Services
{
    public class OrderService
    {
        public int CreateOrder(int userId, List<CartItem> items, string paymentMethod)
        {
            if (items == null || items.Count == 0)
                throw new InvalidOperationException("Корзина пуста.");

            using (var connection = Db.Open(Db.AppConnection))
            using (var tx = connection.BeginTransaction())
            {
                try
                {
                    decimal total = 0;
                    foreach (var item in items) total += item.Total;

                    int orderId;
                    using (var orderCmd = new SqlCommand(@"
INSERT INTO Orders(UserId,ReserveUntil,PaymentMethod,Status,TotalAmount)
OUTPUT INSERTED.Id
VALUES(@u,DATEADD(HOUR,24,SYSDATETIME()),@pm,'Новая',@total)", connection, tx))
                    {
                        orderCmd.Parameters.AddWithValue("@u", userId);
                        orderCmd.Parameters.AddWithValue("@pm", paymentMethod);
                        orderCmd.Parameters.AddWithValue("@total", total);
                        orderId = (int)orderCmd.ExecuteScalar();
                    }

                    foreach (var item in items)
                    {
                        using (var seatsCmd = new SqlCommand("UPDATE Tours SET BookedSeats = BookedSeats + @q WHERE Id=@id AND (MaxGroupSize-BookedSeats) >= @q", connection, tx))
                        {
                            seatsCmd.Parameters.AddWithValue("@q", item.Quantity);
                            seatsCmd.Parameters.AddWithValue("@id", item.TourId);
                            if (seatsCmd.ExecuteNonQuery() == 0)
                                throw new InvalidOperationException("Недостаточно свободных мест в одном из туров.");
                        }

                        using (var itemCmd = new SqlCommand(@"
INSERT INTO OrderItems(OrderId,TourId,Quantity,UnitPrice,Insurance,Transfer,TransferFee,ItemTotal)
VALUES(@o,@t,@q,@p,@i,@tr,@tf,@it)", connection, tx))
                        {
                            itemCmd.Parameters.AddWithValue("@o", orderId);
                            itemCmd.Parameters.AddWithValue("@t", item.TourId);
                            itemCmd.Parameters.AddWithValue("@q", item.Quantity);
                            itemCmd.Parameters.AddWithValue("@p", item.BasePrice);
                            itemCmd.Parameters.AddWithValue("@i", item.Insurance);
                            itemCmd.Parameters.AddWithValue("@tr", item.Transfer);
                            itemCmd.Parameters.AddWithValue("@tf", item.TransferFee);
                            itemCmd.Parameters.AddWithValue("@it", item.Total);
                            itemCmd.ExecuteNonQuery();
                        }
                    }

                    tx.Commit();
                    return orderId;
                }
                catch
                {
                    tx.Rollback();
                    throw;
                }
            }
        }

        public DataTable GetOrders(int? userId = null)
        {
            using (var connection = Db.Open(Db.AppConnection))
            using (var command = new SqlCommand(@"
SELECT o.Id, u.FullName AS Client, o.CreatedAt, o.ReserveUntil, o.PaymentMethod, o.Status, o.TotalAmount,
       g.FullName AS Guide, t.Name AS Transport
FROM Orders o
INNER JOIN Users u ON u.Id=o.UserId
LEFT JOIN Guides g ON g.Id=o.GuideId
LEFT JOIN Transports t ON t.Id=o.TransportId
WHERE (@u IS NULL OR o.UserId=@u)
ORDER BY o.CreatedAt DESC", connection))
            {
                command.Parameters.AddWithValue("@u", (object)userId ?? DBNull.Value);
                var table = new DataTable();
                using (var adapter = new SqlDataAdapter(command)) adapter.Fill(table);
                return table;
            }
        }

        public void MarkPaid(int orderId)
        {
            using (var connection = Db.Open(Db.AppConnection))
            using (var command = new SqlCommand("UPDATE Orders SET Status='Оплачена клиентом' WHERE Id=@id", connection))
            {
                command.Parameters.AddWithValue("@id", orderId);
                command.ExecuteNonQuery();
            }
        }

        public void ConfirmByManager(int orderId, int guideId, int transportId)
        {
            using (var connection = Db.Open(Db.AppConnection))
            {
                using (var guideCmd = new SqlCommand(@"
SELECT COUNT(1)
FROM Orders
WHERE GuideId=@g
  AND Status IN ('Оплачена','Оплачена клиентом')", connection))
                {
                    guideCmd.Parameters.AddWithValue("@g", guideId);
                    if ((int)guideCmd.ExecuteScalar() >= 3)
                        throw new InvalidOperationException("Нельзя назначить гида: у него уже 3 активных тура.");
                }

                int requiredSeats;
                using (var reqCmd = new SqlCommand("SELECT ISNULL(SUM(oi.Quantity),0) FROM OrderItems oi WHERE oi.OrderId=@id", connection))
                {
                    reqCmd.Parameters.AddWithValue("@id", orderId);
                    requiredSeats = (int)reqCmd.ExecuteScalar();
                }

                int capacity;
                using (var trCmd = new SqlCommand("SELECT Capacity FROM Transports WHERE Id=@id", connection))
                {
                    trCmd.Parameters.AddWithValue("@id", transportId);
                    capacity = (int)trCmd.ExecuteScalar();
                }

                if (capacity < requiredSeats)
                    throw new InvalidOperationException("Выбранный транспорт не подходит: вместимость меньше размера группы.");

                using (var command = new SqlCommand("UPDATE Orders SET Status='Оплачена', GuideId=@g, TransportId=@t WHERE Id=@id", connection))
                {
                    command.Parameters.AddWithValue("@id", orderId);
                    command.Parameters.AddWithValue("@g", guideId);
                    command.Parameters.AddWithValue("@t", transportId);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
