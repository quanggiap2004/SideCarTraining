using Microsoft.EntityFrameworkCore;
using SideCar.Business.Data;
using SideCar.Business.Entities;
using SideCar.Business.Repositories.Interfaces;

namespace SideCar.Business.Repositories
{
    public class AuthenRepository : GenericRepository<Users>, IAuthenRepository
    {
        public AuthenRepository(ProjectDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<Users?> GetByUsernameAsync(string username)
        {
            return await dbContext.Set<Users>()
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<Users?> GetByRefreshTokenAsync(string hashedToken)
        {
            return await dbContext.Set<Users>()
                .FirstOrDefaultAsync(u => u.RefreshToken == hashedToken);
        }

        public async Task<bool> ExistsAsync(string username, string email, string phoneNumber)
        {
            return await dbContext.Set<Users>()
                .AnyAsync(u => u.Username == username || u.Email == email || u.PhoneNumber == phoneNumber);
        }

        public async Task AddUserAsync(Users user)
        {
            await AddAsync(user);
            await SaveChangesAsync();
        }
    }
}
