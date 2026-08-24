using WebAppMVC.Models;

namespace WebAppMVC.Services
{
    public class UserService
    {
        private readonly List<User> users = new List<User>
        {
            new User
            {
                Id = 1,
                Username = "admin",
                Password = "123",
                Role = "Admin"
            },
            new User
            {
                Id = 2,
                Username = "employee",
                Password = "123",
                Role = "Employee"
            }
        };


        public User? ValidateUser(string username, string password)
        {
            return users.FirstOrDefault(u =>
                u.Username == username &&
                u.Password == password);
        }
    }
}