using ProtoBuf;

namespace Wikiled.Redis.Channels
{
    /// <summary>
    /// Represents an applications identity on the message bus.
    /// </summary>
    [ProtoContract]
    public class Identity
    {
        /// <summary>
        /// The name of the application.
        /// </summary>
        [ProtoMember(1)]
        public string ApplicationId { get; set; }

        /// <summary>
        /// The environment the application is operating within.
        /// </summary>
        [ProtoMember(2)]
        public string Environment { get; set; }

        /// <summary>
        /// The instance of the application.
        /// </summary>
        [ProtoMember(3)]
        public string InstanceId { get; set; }

        /// <summary>
        /// Application version
        /// </summary>
        [ProtoMember(4)]
        public string Version { get; set; }

        public override string ToString()
        {
            return $"Identity[Enviroment={Environment}, Application={ApplicationId}, InstanceId={InstanceId}, Version={Version}]";
        }
    }
}
