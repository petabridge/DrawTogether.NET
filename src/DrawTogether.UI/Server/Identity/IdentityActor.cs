#nullable enable
using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Event;
using Akka.Persistence;
using Microsoft.AspNetCore.Identity;
using static DrawTogether.UI.Server.Identity.IdentityProtocol;

namespace DrawTogether.UI.Server.Identity
{
    public record UserChangedEvent(IWithUserIdentity Msg, bool Changed, string LoggedChanged);
    
    public sealed class IdentityActor : ReceivePersistentActor
    {
        private readonly ILoggingAdapter _log = Context.GetLogger();
        private readonly HashSet<IActorRef> _identityIndexUpdates = new HashSet<IActorRef>();
        
        public IdentityActor(string userId, IActorRef subscriber)
        {
            UserId = userId;
            _identityIndexUpdates.Add(subscriber);

            Recover<UserChangedEvent>(changed =>
            {
                switch (changed.Msg)
                {
                    case DeleteUser _:
                    {
                        _userInfo = DeletedUser;
                        break;
                    }
                    case UpdateUser update:
                    {
                        var (changes, model) = DiffUserChanged(_userInfo, update);
                        _userInfo = model;
                        break;
                    }
                }
            });

            Command<GetUserInfo>(info =>
            {
                Sender.Tell(User);
            });
            
            Command<GetUserByName>(info =>
            {
                Sender.Tell(User);
            });
            
            Command<UpdateUser>(userUpdate =>
            {
                if (_userInfo.IsDeleted())
                {
                    var str = $"Attempted to update deleted user [{UserId}] - change not allowed";
                    _log.Error(str);
                    Sender.Tell(IdentityResult.Failed(new IdentityError(){ Code = "BADDEVELOPER", Description = str}));
                }
                
                var (userChangedRecord, newUser) = DiffUserChanged(_userInfo, userUpdate);
                if (!userChangedRecord.Changed)
                {
                    _log.Debug("Processed no-op change for user [{0}]", UserId);
                    Sender.Tell(IdentityResult.Success);
                    return;
                }
                
                Persist(userChangedRecord, e =>
                {
                    _log.Info(userChangedRecord.LoggedChanged);
                    
                    // special case - need to check if username was changed
                    var boolUserNameChanged = _userInfo.UserName != newUser.UserName;
                    
                    _userInfo = newUser;
                    Sender.Tell(IdentityResult.Success);

                    if (boolUserNameChanged)
                    {
                        foreach (var sub in _identityIndexUpdates)
                        {
                            sub.Tell(_userInfo);
                        }
                    }
                });
            });

            Command<DeleteUser>(del =>
            {
                if (_userInfo.IsDeleted())
                {
                    _log.Debug("Attempted to deleted already deleted user [{0}]", UserId);
                    Sender.Tell(IdentityResult.Success);
                    return;
                }

                var userChangedEvent = new UserChangedEvent(del, true, $"[{DateTime.UtcNow}] deleted user [{UserId}]");
                
                Persist(del, user =>
                {
                    _log.Info("Successfully deleted user [{0}]", UserId);
                    _userInfo = DeletedUser;
                    Sender.Tell(IdentityResult.Success);
                });
            });
            
            Command<ReceiveTimeout>(t =>
            {
                Context.Stop(Self);
            });
        }

        private static (UserChangedEvent userChanged, UserInfo model) DiffUserChanged(UserInfo start,
            UpdateUser changed)
        {
            var updatedProperties = new List<string>();

            var info = changed.UpdatedInfo;
            if (info.UserName != null && (start.UserName == null || !start.UserName.Equals(info.UserName)))
            {
                updatedProperties.Add($"[{DateTime.UtcNow}] Updated username from [{start.UserName}] to [{info.UserName}]");
                start = start with {UserName = info.UserName};
            }
            
            if (info.Email != null && (start.Email == null || !start.Email.Equals(info.Email)))
            {
                updatedProperties.Add($"[{DateTime.UtcNow}] Updated email from [{start.Email}] to [{info.Email}]");
                start = start with {Email = info.Email};
            }
            
            if (info.EmailConfirmed != null && (start.EmailConfirmed == null || !start.EmailConfirmed.Equals(info.EmailConfirmed)))
            {
                updatedProperties.Add($"[{DateTime.UtcNow}] Updated email confirmed from [{start.EmailConfirmed}] to [{info.EmailConfirmed}]");
                start = start with {EmailConfirmed = info.EmailConfirmed};
            }

            return (
                new UserChangedEvent(changed, updatedProperties.Count > 0,
                    string.Join(Environment.NewLine, updatedProperties)), start);
        }
        
        public string UserId { get; }
        private IdentityProtocol.UserInfo _userInfo = IdentityProtocol.NoUser;

        public DTAnonymousUser User => _userInfo.ToAnonUser();

        public override string PersistenceId => UserId;

        protected override void PreStart()
        {
            Context.SetReceiveTimeout(TimeSpan.FromMinutes(20));
        }

        protected override void OnReplaySuccess()
        {
            if (!string.IsNullOrEmpty(_userInfo.UserName))
            {
                foreach (var sub in _identityIndexUpdates)
                {
                    // LET SUBSCRIBERS KNOW ABOUT OUR INITIAL STATE
                    sub.Tell(_userInfo);
                }
            }
           
            base.OnReplaySuccess();
        }
    }
}