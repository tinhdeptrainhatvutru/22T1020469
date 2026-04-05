using SV22T1020469.Models.Security;
using Microsoft.Data.SqlClient;

public static class SecurityDataService
{
    public static UserAccount AuthenticateEmployee(string username, string password)
    {
        using (var connection = new SqlConnection("Server=.;Database=LiteCommerceDB;Trusted_Connection=True;TrustServerCertificate=True;"))
        {
            connection.Open();

            string sql = @"SELECT * FROM Employees 
                           WHERE Email = @email AND Password = @password";

            using (var cmd = new SqlCommand(sql, connection))
            {
                cmd.Parameters.AddWithValue("@email", username);
                cmd.Parameters.AddWithValue("@password", password);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new UserAccount()
                        {
                            UserId = reader["EmployeeID"]?.ToString() ?? "",
                            UserName = reader["Email"]?.ToString() ?? "",
                            DisplayName = reader["FullName"]?.ToString() ?? "",
                            Email = reader["Email"]?.ToString() ?? "",
                            Photo = reader["Photo"]?.ToString() ?? "nophoto.png",
                            RoleNames = "admin"
                        };
                    }
                }
            }
        }
        return null!;
    }
}