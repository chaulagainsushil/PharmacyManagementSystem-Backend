using PMS.Common.DTOs;

namespace PMS.Application.Application.Interface.Repository;

public interface IUserRepository
{
    Task<IEnumerable<UserResponseDto>> GetAllAsync();
}
