#nullable enable
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

        /// <summary>
        /// Special case for users who were deleted.
        /// </summary>
        public static readonly UserInfo DeletedUser = new UserInfo("DELETED");

        public static bool IsNobody(this UserInfo user)
        {
            return user == NoUser;
        }

        public static bool IsDeleted(this UserInfo user)
        {
            return user == DeletedUser;
        }

        public static DTAnonymousUser ToAnonUser(this UserInfo user)
        {
            return new DTAnonymousUser()
            {
                Id = user.UserId, Email = user.Email,
                EmailConfirmed = user.EmailConfirmed ?? false,
                UserName = user.UserName,
            };
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

            public UserInfo UpdatedInfo { get; }


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

        /// <summary>
        /// Should return a <see cref="UserInfo"/> object if the user is found.
        /// </summary>
        public sealed class GetUserByName : IWithUserNameOnly
        {
            public GetUserByName(string userName)
            {
                UserName = userName;
            }

            public string UserName { get; }
        }
    }
}