#nullable enable
using Akka.Actor;
using Akka.Persistence;

namespace DrawTogether.UI.Server.Identity
{
    /// <summary>
    /// DEFINES IDENTITY CRUD PROTOCOL
    /// </summary>
    public static class IdentityProtocol
    {
        public interface IWithUserIdentity
        {
            string UserId { get; }
        }

        public interface IWithUserNameOnly
        {
            string UserName { get; }
        }

        public record UserInfo(string UserId, string? UserName = null, string? Email = null,
            bool? EmailConfirmed = null)
        {
            public string UserId { get; } = UserId;
        }

        /// <summary>
        /// Special case that indicates that this user was not found.
        /// </summary>
        public static readonly UserInfo NoUser = new UserInfo(string.Empty);

        public static bool IsNobody(this UserInfo user)
        {
            return user == NoUser;
        }

        /// <summary>
        /// Creates or updates user properties
        /// </summary>
        public sealed class UpdateUser : IWithUserIdentity
        {
            public UpdateUser(UserInfo updatedInfo)
            {
                UpdatedInfo = updatedInfo;
            }

            private UserInfo UpdatedInfo { get; }


            public string UserId => UpdatedInfo.UserId;
        }

        /// <summary>
        /// Soft-deletes a user from our system
        /// </summary>
        public sealed class DeleteUser : IWithUserIdentity
        {
            public DeleteUser(string userId)
            {
                UserId = userId;
            }

            public string UserId { get; }
        }

        /// <summary>
        /// Should return a <see cref="UserInfo"/> object if the user is found.
        /// </summary>
        public sealed class GetUserInfo : IWithUserIdentity
        {
            public GetUserInfo(string userId)
            {
                UserId = userId;
            }

            public string UserId { get; }
        }

        public sealed class GetUserByName : IWithUserNameOnly
        {
            public GetUserByName(string userName)
            {
                UserName = userName;
            }

            public string UserName { get; }
        }
    }
    
    public sealed class IdentityActor : ReceivePersistentActor
    {
        public IdentityActor(string userId)
        {
            UserId = userId;
        }
        
        public string UserId { get; }

        public override string PersistenceId => UserId;
    }
}