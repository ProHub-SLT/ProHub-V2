using ProHub.Models;

namespace ProHub.Data.Interfaces
{
    public interface IEmployeePermissionRepository
    {
        Employee GetEmployeeByEmail(string email);
    }
}
