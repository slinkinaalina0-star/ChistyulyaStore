// В основном проекте ChistyulyaStore создайте новый файл BusinessLogic.cs
using ChistyulyaStore.Models;
using System.Linq;

namespace ChistyulyaStore.Business
{
    public static class AuthLogic
    {
        public static bool ValidateUser(string email, string password)
        {
            using (var db = new DBEntities())
            {
                var user = db.Users.FirstOrDefault(u => u.Email == email && u.Password == password);
                return user != null;
            }
        
    }


public static int GetUserRole(string email, string password)
        {
            using (var db = new DBEntities())
            {
                var user = db.Users.FirstOrDefault(u => u.Email == email && u.Password == password);
                return user?.ID_EmployeeRole ?? 0;
            }
        }
    }
}