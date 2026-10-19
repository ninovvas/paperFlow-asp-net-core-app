namespace PaperFlow.Services
{

    using Microsoft.EntityFrameworkCore;

    using PaperFlow.Data;
    using PaperFlow.Services.Interfaces;

    public sealed class PaperDeleteService : IPaperDeleteService
    {
        private readonly PowerFlowDbContext context;

        public PaperDeleteService(PowerFlowDbContext context)
        {
            this.context = context;
        }

        public async Task<bool> DeleteAsync(
            int id,
            string userId,
            CancellationToken cancellationToken = default)
        {
            ValidateUserId(userId);

            if (id <= 0)
            {
                return false;
            }

            var paper = await context.Papers
                .FirstOrDefaultAsync(
                    p => p.Id == id && p.UserId == userId,
                    cancellationToken);

            if (paper == null)
            {
                return false;
            }

            context.Papers.Remove(paper);

            await context.SaveChangesAsync(cancellationToken);

            return true;
        }

   

        public async Task<int> CountAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            ValidateUserId(userId);

            return await context.Papers
                .AsNoTracking()
                .CountAsync(
                    paper => paper.UserId == userId,
                    cancellationToken);
        }


        public async Task<int> DeleteAllAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            ValidateUserId(userId);

            int deletedCount = await context.Papers
                .Where(p => p.UserId == userId)
                .ExecuteDeleteAsync(cancellationToken);

            return deletedCount;
        }

    
        private static void ValidateUserId(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new ArgumentException("Authenticated user ID is required.", nameof(userId));
            }
        }
    }
}
