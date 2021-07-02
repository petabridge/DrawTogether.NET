using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.AspNetCore.Identity;

namespace DrawTogether.UI.Server.Identity
{
    public class AkkaUserStore : IUserStore<DTAnonymousUser>
    {
        /// <summary>
        /// Going to function as an aggregate root for the entire identity system.
        /// </summary>
        /// <remarks>
        /// In a clustered environment, this will be probably a ShardRegion.
        ///
        /// In local environment, this will be a parent actor in the child-per-entity pattern.
        /// </remarks>
        private readonly IActorRef _identityActorRoot;

        public AkkaUserStore(IActorRef identityActorRoot)
        {
            _identityActorRoot = identityActorRoot;
        }

        public void Dispose()
        {
            
        }

        public Task<string> GetUserIdAsync(DTAnonymousUser user, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task<string> GetUserNameAsync(DTAnonymousUser user, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task SetUserNameAsync(DTAnonymousUser user, string userName, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task<string> GetNormalizedUserNameAsync(DTAnonymousUser user, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task SetNormalizedUserNameAsync(DTAnonymousUser user, string normalizedName, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task<IdentityResult> CreateAsync(DTAnonymousUser user, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task<IdentityResult> UpdateAsync(DTAnonymousUser user, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task<IdentityResult> DeleteAsync(DTAnonymousUser user, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task<DTAnonymousUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public Task<DTAnonymousUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}