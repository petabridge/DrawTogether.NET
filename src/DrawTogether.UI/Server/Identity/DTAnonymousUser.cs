using Microsoft.AspNetCore.Identity;

namespace DrawTogether.UI.Server.Identity
{
    public class DTAnonymousUser : IdentityUser
    {

    }

    public static class DTAnonymousUserExtensions
    {
        public static bool IsNotRealUser(this DTAnonymousUser user)
        {
            return user.Id.Equals(IdentityProtocol.NoUser.UserId) ||
                   user.Id.Equals(IdentityProtocol.DeletedUser.UserId);
        }
    }
}