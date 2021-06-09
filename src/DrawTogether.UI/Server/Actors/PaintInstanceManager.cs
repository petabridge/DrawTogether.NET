// -----------------------------------------------------------------------
// <copyright file="PaintInstanceManager.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2021 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.Actor;
using Akka.DependencyInjection;
using DrawTogether.UI.Shared.Connectivity;

namespace DrawTogether.UI.Server.Actors
{
    public sealed class PaintInstanceManager : UntypedActor
    {
        private ServiceProvider _sp;

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case PaintSessionProtocol.IPaintSessionMessage m:
                {
                    // need to create or get child that corresponds to sessionId
                    var child = Context.Child(m.InstanceId)
                        .GetOrElse(() => Context.ActorOf(_sp.Props<PaintInstanceActor>(m.InstanceId), m.InstanceId));

                    child.Forward(m);
                    break;
                }
                default:
                    Unhandled(message);
                    break;
            }
        }

        protected override void PreStart()
        {
            _sp = ServiceProvider.For(Context.System);
        }
    }
}