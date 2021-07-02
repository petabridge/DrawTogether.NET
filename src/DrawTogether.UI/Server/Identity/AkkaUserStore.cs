using System;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Microsoft.AspNetCore.Identity;
using static DrawTogether.UI.Server.Identity.IdentityProtocol;

namespace DrawTogether.UI.Server.Identity
{
    /// <summary>
    /// Akka.Persistence-backed ASP.NET User Store
    /// </summary>
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

        public async Task<string> GetUserIdAsync(DTAnonymousUser user, CancellationToken cancellationToken)
        {
            if (user.Id != null)
                return (await _identityActorRoot.Ask<DTAnonymousUser>(new GetUserInfo(user.Id), cancellationToken)).Id;
            if (user.UserName != null)
                return (await _identityActorRoot.Ask<DTAnonymousUser>(new GetUserByName(user.UserName),
                    cancellationToken)).Id;

            throw new NotSupportedException("Must have a userId or userName specified in request.");
        }

        public async Task<string> GetUserNameAsync(DTAnonymousUser user, CancellationToken cancellationToken)
        {
            if (user.Id != null)
                return (await _identityActorRoot.Ask<DTAnonymousUser>(new GetUserInfo(user.Id), cancellationToken)).UserName;
            
            throw new NotSupportedException("Must have a userId specified in request.");
        }

        public async Task SetUserNameAsync(DTAnonymousUser user, string userName, CancellationToken cancellationToken)
        {
            if (user.Id == null)
                throw new NotSupportedException("Must have a userId specified in request.");
            
            await _identityActorRoot.Ask<IdentityResult>(new UpdateUser(new UserInfo(user.Id, userName)),
                cancellationToken);
        }

        public async Task<string> GetNormalizedUserNameAsync(DTAnonymousUser user, CancellationToken cancellationToken)
        {
            if (user.Id != null)
                return (await _identityActorRoot.Ask<DTAnonymousUser>(new GetUserInfo(user.Id), cancellationToken)).NormalizedUserName;
            
            throw new NotSupportedException("Must have a userId specified in request.");
        }

        public async Task SetNormalizedUserNameAsync(DTAnonymousUser user, string normalizedName, CancellationToken cancellationToken)
        {
            if (user.Id == null)
                throw new NotSupportedException("Must have a userId specified in request.");
            
            await _identityActorRoot.Ask<IdentityResult>(new UpdateUser(new UserInfo(user.Id, normalizedName)),
                cancellationToken);
        }

        public async Task<IdentityResult> CreateAsync(DTAnonymousUser user, CancellationToken cancellationToken)
        {
            if (user.Id == null)
                throw new NotSupportedException("Must have a userId specified in request.");
            
            return await _identityActorRoot.Ask<IdentityResult>(new UpdateUser(new UserInfo(user.Id)),
                cancellationToken);
        }

        public async Task<IdentityResult> UpdateAsync(DTAnonymousUser user, CancellationToken cancellationToken)
        {
            if (user.Id == null)
                throw new NotSupportedException("Must have a userId specified in request.");
            
            return await _identityActorRoot.Ask<IdentityResult>(new UpdateUser(new UserInfo(user.Id, user.UserName, user.Email, user.EmailConfirmed)),
                cancellationToken);
        }

        public async Task<IdentityResult> DeleteAsync(DTAnonymousUser user, CancellationToken cancellationToken)
        {
            if (user.Id == null)
                throw new NotSupportedException("Must have a userId specified in request.");
            
            return await _identityActorRoot.Ask<IdentityResult>(new DeleteUser(user.Id),
                cancellationToken);
        }

        public async Task<DTAnonymousUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(userId))
                return (await _identityActorRoot.Ask<DTAnonymousUser>(new GetUserInfo(userId), cancellationToken));
            
            throw new NotSupportedException("Must have a userId specified in request.");
        }

        public async Task<DTAnonymousUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(normalizedUserName))
                return (await _identityActorRoot.Ask<DTAnonymousUser>(new GetUserByName(normalizedUserName), cancellationToken));
            
            throw new NotSupportedException("Must have a userName specified in request.");
        }
    }
}