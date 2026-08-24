using System.Net.Http.Json;
using WebAppMVC.Models;

namespace WebAppMVC.Services
{
    public class EmployeeService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public EmployeeService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<Employee>> GetAllEmployees()
        {
            var client = _httpClientFactory.CreateClient("WebInterfaceApi");
            var response = await client.GetAsync("api/Employees");

            if (!response.IsSuccessStatusCode)
                throw new Exception("API call failed: " + response.StatusCode);

            var json = await response.Content.ReadAsStringAsync();
            var employees = System.Text.Json.JsonSerializer.Deserialize<List<Employee>>(json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return employees ?? new List<Employee>();
        }

        public async Task<Employee?> GetEmployeeById(int id)
        {
            var client = _httpClientFactory.CreateClient("WebInterfaceApi");
            var response = await client.GetAsync($"api/Employees/{id}");

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            if (!response.IsSuccessStatusCode)
                throw new Exception("API call failed: " + response.StatusCode);

            var json = await response.Content.ReadAsStringAsync();
            return System.Text.Json.JsonSerializer.Deserialize<Employee>(json,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public async Task AddEmployee(Employee employee)
        {
            var client = _httpClientFactory.CreateClient("WebInterfaceApi");
            var response = await client.PostAsJsonAsync("api/Employees", employee);

            if (!response.IsSuccessStatusCode)
                throw new Exception("API call failed: " + response.StatusCode);
        }

        public async Task UpdateEmployee(Employee employee)
        {
            var client = _httpClientFactory.CreateClient("WebInterfaceApi");
            var response = await client.PutAsJsonAsync($"api/Employees/{employee.Id}", employee);

            if (!response.IsSuccessStatusCode)
                throw new Exception("API call failed: " + response.StatusCode);
        }

        public async Task DeleteEmployee(int id)
        {
            var client = _httpClientFactory.CreateClient("WebInterfaceApi");
            var response = await client.DeleteAsync($"api/Employees/{id}");

            if (!response.IsSuccessStatusCode)
                throw new Exception("API call failed: " + response.StatusCode);
        }
    }
}