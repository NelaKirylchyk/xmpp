// --------------------------------------------------------------------------------------------------------------------
// <copyright company="" file="SASLState.cs">
//   
// </copyright>
// <summary>
//   The sasl state.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------

using System.Linq;
using XMPP.SASL;
using XMPP.Tags;
using XMPP.Tags.XmppSasl;
using XMPP.�ommon;

namespace XMPP.States
{
    public class SaslState : IState
    {
        public SaslState(Manager manager) : base(manager)
        {
        }

        public override void Execute(Tag data = null)
        {
#if DEBUG
            Manager.Events.LogMessage(this, LogType.Debug, "Processing next SASL step");
#endif
            Tag res = Manager.SaslProcessor.Step(data);
            switch (res.Name.LocalName)
            {
                case "success":
                {
#if DEBUG
                    Manager.Events.LogMessage(this, LogType.Debug, "Success, sending start stream again");
#endif
                    Manager.IsAuthenticated = true;

                    if (Manager.Transport == Transport.Socket)
                    {
                        Manager.State = new ConnectedState(Manager);
                        Manager.State.Execute();
                    }
                    else
                    {
                        (Manager.Connection as BoSH).Restart();
                    }

                    break;
                }

                case "failure":
                {
                    var type = ErrorType.Undefined;

                    if (Manager.SaslProcessor is MD5Processor)
                        type = ErrorType.Md5AuthError;

                    if (Manager.SaslProcessor is PlainProcessor)
                        type = ErrorType.PlainAuthError;

                    if (Manager.SaslProcessor is SCRAMProcessor)
                        type = ErrorType.ScramAuthError;

                    if (Manager.SaslProcessor is XOAUTH2Processor)
                        type = ErrorType.Oauth2AuthError;

                    var failure = data as Failure;
                    string text = string.Empty;
                    if (failure.TextElements.Any())
                        text = failure.TextElements.First().Value;

                    Manager.Events.Error(this, type, ErrorPolicyType.Deactivate, text);

                    return;
                }

                default:
                {
                    Manager.Connection.Send(res);
                    break;
                }
            }
        }
    }
}