using System;
using System.Data;
using System.Data.SqlClient;
using Turfirm.Infrastructure;

namespace Turfirm.Services
{
    public class TourService
    {
        public DataTable GetTours(string direction, string type, DateTime? from, DateTime? to, decimal? maxPrice, string search)
        {
            using (var connection = Db.Open(Db.AppConnection))
            using (var command = new SqlCommand(@"
SELECT Id, Title, Direction, TourType, StartDate, EndDate,
       BasePrice, OldPrice, DiscountPercent,
       (MaxGroupSize - BookedSeats) AS FreeSeats,
       ImagePath, Description
FROM Tours
WHERE (@direction = '' OR Direction = @direction)
  AND (@type = '' OR TourType = @type)
  AND (@from IS NULL OR StartDate >= @from)
  AND (@to IS NULL OR EndDate <= @to)
  AND (@maxPrice IS NULL OR BasePrice <= @maxPrice)
  AND (@search = '' OR Title LIKE '%' + @search + '%' OR Description LIKE '%' + @search + '%')
ORDER BY StartDate", connection))
            {
                command.Parameters.AddWithValue("@direction", direction ?? string.Empty);
                command.Parameters.AddWithValue("@type", type ?? string.Empty);
                command.Parameters.AddWithValue("@from", (object)from ?? DBNull.Value);
                command.Parameters.AddWithValue("@to", (object)to ?? DBNull.Value);
                command.Parameters.AddWithValue("@maxPrice", (object)maxPrice ?? DBNull.Value);
                command.Parameters.AddWithValue("@search", search ?? string.Empty);

                var table = new DataTable();
                using (var adapter = new SqlDataAdapter(command))
                {
                    adapter.Fill(table);
                }
                return table;
            }
        }

        public DataTable GetAll(string tableName)
        {
            using (var connection = Db.Open(Db.AppConnection))
            using (var adapter = new SqlDataAdapter($"SELECT * FROM {tableName}", connection))
            {
                var table = new DataTable();
                adapter.Fill(table);
                return table;
            }
        }

        public void Upsert(string tableName, DataRow row)
        {
            using (var connection = Db.Open(Db.AppConnection))
            {
                if (Convert.ToInt32(row["Id"]) == 0)
                {
                    if (tableName == "Tours")
                    {
                        using (var cmd = new SqlCommand(@"INSERT INTO Tours(Title,Direction,TourType,StartDate,EndDate,MaxGroupSize,BookedSeats,BasePrice,OldPrice,DiscountPercent,ImagePath,Description)
VALUES(@Title,@Direction,@TourType,@StartDate,@EndDate,@MaxGroupSize,@BookedSeats,@BasePrice,@OldPrice,@DiscountPercent,@ImagePath,@Description)", connection))
                        {
                            FillTourParams(cmd, row);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
                else
                {
                    if (tableName == "Tours")
                    {
                        using (var cmd = new SqlCommand(@"UPDATE Tours SET Title=@Title,Direction=@Direction,TourType=@TourType,StartDate=@StartDate,EndDate=@EndDate,
MaxGroupSize=@MaxGroupSize,BookedSeats=@BookedSeats,BasePrice=@BasePrice,OldPrice=@OldPrice,DiscountPercent=@DiscountPercent,
ImagePath=@ImagePath,Description=@Description WHERE Id=@Id", connection))
                        {
                            cmd.Parameters.AddWithValue("@Id", row["Id"]);
                            FillTourParams(cmd, row);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }

        public void DeleteTour(int id)
        {
            using (var connection = Db.Open(Db.AppConnection))
            {
                using (var check = new SqlCommand("SELECT COUNT(1) FROM OrderItems WHERE TourId=@id", connection))
                {
                    check.Parameters.AddWithValue("@id", id);
                    if ((int)check.ExecuteScalar() > 0)
                        throw new InvalidOperationException("Невозможно удалить товар, так как он присутствует в одном или нескольких заказах.");
                }

                using (var command = new SqlCommand("DELETE FROM Tours WHERE Id=@id", connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.ExecuteNonQuery();
                }
            }
        }

        private static void FillTourParams(SqlCommand cmd, DataRow row)
        {
            cmd.Parameters.AddWithValue("@Title", row["Title"]);
            cmd.Parameters.AddWithValue("@Direction", row["Direction"]);
            cmd.Parameters.AddWithValue("@TourType", row["TourType"]);
            cmd.Parameters.AddWithValue("@StartDate", Convert.ToDateTime(row["StartDate"]));
            cmd.Parameters.AddWithValue("@EndDate", Convert.ToDateTime(row["EndDate"]));
            cmd.Parameters.AddWithValue("@MaxGroupSize", row["MaxGroupSize"]);
            cmd.Parameters.AddWithValue("@BookedSeats", row["BookedSeats"]);
            cmd.Parameters.AddWithValue("@BasePrice", row["BasePrice"]);
            cmd.Parameters.AddWithValue("@OldPrice", row["OldPrice"] == DBNull.Value ? (object)DBNull.Value : row["OldPrice"]);
            cmd.Parameters.AddWithValue("@DiscountPercent", row["DiscountPercent"] == DBNull.Value ? (object)DBNull.Value : row["DiscountPercent"]);
            cmd.Parameters.AddWithValue("@ImagePath", row["ImagePath"] == DBNull.Value ? (object)DBNull.Value : row["ImagePath"]);
            cmd.Parameters.AddWithValue("@Description", row["Description"] == DBNull.Value ? (object)DBNull.Value : row["Description"]);
        }
    }
}
