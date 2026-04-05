using SV22T1020469.DataLayers.Interfaces;
using SV22T1020469.DataLayers.SQLServer;
using SV22T1020469.Models.Security;
using System.Threading.Tasks;

namespace SV22T1020469.BusinessLayers
{
    public static class UserAccountDataService
    {
        private static readonly IUserAccountRepository employeeDB;
        private static readonly IUserAccountRepository customerDB;

        static UserAccountDataService()
        {
            employeeDB =
                new EmployeeAccountRepository(
                    Configuration.ConnectionString);

            customerDB =
                new CustomerAccountRepository(
                    Configuration.ConnectionString);
        }

        public static async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            var employee =
                await employeeDB.AuthorizeAsync(
                    userName,
                    password);  

            if (employee != null)
                return employee;

            return await customerDB.AuthorizeAsync(
                    userName,
                    password);
        }

        public static async Task<bool> ChangePasswordAsync(
            string userName,
            string password)
        {
            userName = userName?.Trim() ?? "";
            password = password?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                return false;

            // Update by username directly. Do not re-authorize using new password.
            bool employeeChanged = await employeeDB.ChangePasswordAsync(userName, password);
            if (employeeChanged)
                return true;

            return await customerDB.ChangePasswordAsync(userName, password);
        }
    }
}

