using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GOCore;
namespace Services;
public interface IUserDBService
{
    Task<Customer> CreateCustomerAsync(Customer customer);
    Task<Customer> GetCustomerByIdAsync(string id);
    Task<IEnumerable<Customer>> GetAllCustomersAsync();
    Task<Customer> UpdateCustomerAsync(string id, Customer updatedCustomer);
    Task<bool> DeleteCustomerAsync(string id);
}